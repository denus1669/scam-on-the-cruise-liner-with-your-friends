using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Универсальный компонент для управления мухлежом.
/// Добавляется на префаб Бота ИЛИ префаб Игрока.
/// </summary>
public class CheatController : NetworkBehaviour
{
    [Header("Настройки мухлежа")]
    [Tooltip("Список доступных видов мухлежа для этого персонажа.")]
    [SerializeField] private List<CheatAction> availableCheats = new List<CheatAction>();

    // Сетевое состояние мухлежа
    private NetworkVariable<bool> isCheating = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private CheatAction currentCheatAction;
    private Coroutine cheatCoroutine;

    // Ссылки на компоненты
    private Animator characterAnimator;
    private BotAgent botAgent;
    private BotDispleasureController displeasureController;

    // Флаг, определяющий, кто владелец этого скрипта
    private bool isBot;

    // Сохраняем текущий контекст мухлежа, чтобы иметь доступ к столу при обвинении
    private CheatContext activeContext;

    public bool IsCheating => isCheating.Value;

    // --- ГЛОБАЛЬНЫЕ СОБЫТИЯ ДЛЯ ВИЗУАЛА (срабатывают на всех клиентах) ---
    public event Action<string> OnCheatStarted;
    public event Action OnCheatCanceled;
    public event Action<bool> OnCheatingStateChanged;

    // --- СОБЫТИЯ ТОЛЬКО ДЛЯ ЛОКАЛЬНОГО ИГРОКА (Владельца) ---
    public event Action<CheatAction> OnLocalPlayerCheatUIRequested;

    private void Awake()
    {
        characterAnimator = GetComponentInChildren<Animator>();

        // Если на объекте есть BotAgent, значит этот контроллер принадлежит боту.
        isBot = TryGetComponent<BotAgent>(out botAgent);
        displeasureController = GetComponent<BotDispleasureController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isCheating.OnValueChanged += HandleCheatingStateChanged;
        OnCheatingStateChanged?.Invoke(isCheating.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        isCheating.OnValueChanged -= HandleCheatingStateChanged;
    }

    public bool CanCheat()
    {
        // Проверяем, что персонаж не в процессе мухлежа и что он владелец (или бот)
        return !isCheating.Value && (IsOwner || isBot);
    }

    /// <summary>
    /// Пытается начать мухлеж. 
    /// Бот будет передавать specificCheat = null, чтобы выбрать случайный мухлеж.
    /// Игрок может передавать конкретный specificCheat, выбранный через UI.
    /// </summary>
    public bool TryInitiateCheat(bool isBot, IGameTable table, CheatAction specificCheat = null)
    {
        Debug.Log($"[CheatController] Попытка начать мухлеж. isBot={isBot}, specificCheat={specificCheat?.CheatName ?? "null"}");
        if (!IsServer || isCheating.Value) return false;

        // 1. Формируем правильный контекст
        CheatContext context = isBot
            ? new CheatContext(table, botAgent)
            : new CheatContext(table, OwnerClientId);

        CheatAction cheatToExecute = specificCheat;

        // 2. Если мухлеж не указан явно (например, это ИИ), выбираем подходящий случайно
        if (cheatToExecute == null)
        {
            cheatToExecute = GetRandomValidCheat(context);
            if (cheatToExecute == null)
            {
                Debug.Log("Нет доступных вариантов мухлежа для текущей ситуации.");
                return false; // Нет доступных вариантов
            }
        }
        else
        {
            if (!cheatToExecute.CanExecute(context)) return false;
        }

        activeContext = context;

        // 3. Запускаем процесс
        cheatCoroutine = StartCoroutine(CheatRoutine(cheatToExecute, context));
        return true;
    }
    public IEnumerator BotCheatRoutine(CheatAction cheat, CheatContext context)
    {

        // Бот просто ждёт AnimationDuration — это окно для обвинения игроком
        float timer = cheat.AnimationDuration;
        while (timer > 0)
        {
            timer -= Time.deltaTime;

            if (context.Table == null || !context.Table.IsGameStarted)
            {
                CancelCheat();
                yield break;
            }
            yield return null;
        }

        // Мухлеж бота удался (игрок его не поймал)
        CompleteCheat(cheat, context);
    }
    public IEnumerator PlayerCheatRoutine(CheatAction cheat, CheatContext context)
    {
        //Даем команду локальному клиенту открыть UI мини - игры
        StartPlayerMinigameClientRpc(cheat.name);

        // Сервер включает тайм-аут (AnimationDuration + 5 секунд форы), 
        // чтобы игрок не держал окно открытым вечно.
        float maxWaitTime = cheat.AnimationDuration + 5f;
        while (maxWaitTime > 0)
        {
            maxWaitTime -= Time.deltaTime;

            if (context.Table == null || !context.Table.IsGameStarted)
            {
                CancelCheat();
                yield break;
            }
            yield return null; // Ждем ответа от FinishPlayerCheatServerRpc
        }

        // Если время вышло, а игрок так и не прислал результат - наказываем
        Debug.LogWarning($"[CheatController] Игрок {OwnerClientId} не завершил мини-игру вовремя.");
        HandleCheatCaught(ulong.MaxValue); // Игрок "выдал" сам себя
    }

    private IEnumerator CheatRoutine(CheatAction cheat, CheatContext context)
    {
        isCheating.Value = true;
        currentCheatAction = cheat;
        
        string actorName = isBot ? $"Бот {gameObject.name}" : $"Игрок {OwnerClientId}";
        Debug.Log($"[CheatController] {actorName} начинает мухлевать: {cheat.CheatName}");

        // Оповещаем всех клиентов (запуск подозрительной анимации и звуков на клиентах)
        NotifyCheatStartedClientRpc(cheat.AnimationTriggerName);

        if (isBot)
        {
            yield return BotCheatRoutine(cheat, context);

        }
        else
        {
            yield return PlayerCheatRoutine(cheat, context);
        }
    }


    private void CompleteCheat(CheatAction cheat, CheatContext context)
    {
        Debug.Log($"[CheatController] Мухлеж '{cheat.CheatName}' успешен для !");
        cheat.ApplyCheatResult(context);

        isCheating.Value = false;
        currentCheatAction = null;
        activeContext = null;
    }

    [ClientRpc]
    private void StartPlayerMinigameClientRpc(string cheatActionName)
    {
        // Открываем UI только у владельца этого персонажа
        if (!IsOwner || isBot) return;

        Debug.Log("[CheatController] Получен запрос от сервера: Открыть UI мини-игры.");

        // Находим нужный SO по имени в нашем локальном списке, чтобы передать его в UI
        CheatAction match = availableCheats.Find(c => c.name == cheatActionName);
        if (match != null)
        {
            OnLocalPlayerCheatUIRequested?.Invoke(match);
        }
    }

    /// <summary>
    /// Вызывается клиентом (из UI Менеджера), когда он успешно прошел или завалил мини-игру.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void FinishPlayerCheatServerRpc(bool isSuccess)
    {
        if (!IsServer || !isCheating.Value || isBot) return;

        // Останавливаем корутину ожидания (тайм-аут)
        if (cheatCoroutine != null) StopCoroutine(cheatCoroutine);

        if (isSuccess)
        {
            CompleteCheat(currentCheatAction, activeContext);
        }
        else
        {
            Debug.Log($"[CheatController] Игрок {OwnerClientId} завалил мини-игру.");
            // Игрок ошибся — это приравнивается к поимке за руку (побеждает казино/оппонент)
            HandleCheatCaught(ulong.MaxValue);
        }
    }

    private void CancelCheat()
    {
        if (cheatCoroutine != null) StopCoroutine(cheatCoroutine);
        isCheating.Value = false;
        currentCheatAction = null;
        activeContext = null;
        StopCheatAnimationClientRpc();
    }

    /// <summary>
    /// Обработка последствий успешной поимки за руку.
    /// </summary>
    public void HandleCheatCaught(ulong accuserClientId)
    {
        if (activeContext == null || activeContext.Table == null) return;

        string cheaterName = isBot ? $"Бот {gameObject.name}" : $"Игрок {OwnerClientId}";
        Debug.Log($"[CheatSystem] {cheaterName} ПОЙМАН ЗА РУКУ! Делегируем обработку столу.");

        // 1. Оповещаем всех клиентов (для UI-эффектов, звуков)
        GameInterruptedClientRpc(accuserClientId, isBot);

        // 2. Делегируем столу решение (форс-стоп, списание денег, уход бота)
        // ВАЖНО: Мы полностью сохранили твою чистую логику OnCheaterCaught!
        activeContext.Table.OnCheaterCaught(accuserClientId, isBot);

        // 3. Сбрасываем состояние мухлежа
        CancelCheat();
    }

    [ClientRpc]
    private void GameInterruptedClientRpc(ulong accuserClientId, bool cheaterIsBot)
    {
        string cheaterName = cheaterIsBot ? $"Бот {gameObject.name}" : $"Игрок {OwnerClientId}";
        Debug.Log($"[UI/FX] ИГРА ПРЕРВАНА! Игрок {accuserClientId} поймал за руку: {cheaterName}!");
    }

    /// <summary>
    /// Вызывается, когда кто-то пытается поймать персонажа за руку (через нажатие E).
    /// </summary>
    [Rpc(SendTo.Server)]
    public void AccuseServerRpc(ulong accuserClientId)
    {
        if (!IsServer) return;

        string actorName = isBot ? "Бот" : $"Игрок {OwnerClientId}";

        if (isCheating.Value)
        {
            Debug.Log($"[CheatSystem] Игрок {accuserClientId} успешно поймал за руку: {actorName}!");
            HandleCheatCaught(accuserClientId);
        }
        else
        {
            Debug.Log($"[CheatSystem] Игрок {accuserClientId} ложно обвинил: {actorName}.");
            // Ложное обвинение (влияет на репутацию, штраф или раздражение бота)
        }
    }

    private CheatAction GetRandomValidCheat(CheatContext context)
    {
        List<CheatAction> validCheats = new List<CheatAction>();
        int totalWeight = 0;

        foreach (var cheat in availableCheats)
        {
            if (cheat.CanExecute(context))
            {
                validCheats.Add(cheat);
                totalWeight += cheat.SelectionWeight;
            }
        }

        if (validCheats.Count == 0) return null;

        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        foreach (var cheat in validCheats)
        {
            randomValue -= cheat.SelectionWeight;
            if (randomValue < 0) return cheat;
        }

        return validCheats[validCheats.Count - 1];
    }

    /// <summary>
    /// Вызывается локальным игроком, чтобы запросить запуск мухлежа.
    /// Сервер сам определит, за каким столом сидит игрок, используя GameTableManager.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void RequestCheatServerRpc(bool isBot, string specificCheatName = "")
    {
        Debug.Log($"[CheatController] Игрок {OwnerClientId} запросил мухлеж. isBot={isBot}, specificCheat={specificCheatName}");
        if (!CanCheat()) return;
        Debug.Log($"[CheatController] Сервер обрабатывает запрос мухлежа от {OwnerClientId}.");
        if (!IsServer || isCheating.Value) return;

        // 1. Ищем стол игрока через кешированный список менеджера 
        IGameTable currentTable = null;
        if (GameTableManager.Instance != null)
        {
            currentTable = GameTableManager.Instance.GetTableOccupiedByPlayer(OwnerClientId);
        }

        // Если игрок нажал "мухлевать", но он не за столом — игнорируем
        if (currentTable == null)
        {
            Debug.LogWarning($"[CheatController] Игрок {OwnerClientId} пытается мухлевать, но он не сидит за столом!");
            return;
        }

        // 2. Определяем конкретный чит
        CheatAction cheatToExecute = null;
        if (!string.IsNullOrEmpty(specificCheatName))
        {
            cheatToExecute = availableCheats.Find(c => c.name == specificCheatName);
        }

        // 3. Запускаем серверную логику мухлежа
        TryInitiateCheat(isBot, currentTable, cheatToExecute);
    }

    private void HandleCheatingStateChanged(bool previous, bool current)
    {
        OnCheatingStateChanged?.Invoke(current);
    }

    [ClientRpc]
    private void NotifyCheatStartedClientRpc(string animationTriggerName)
    {
    }

    [ClientRpc]
    private void PlayCheatAnimationClientRpc(string animationTrigger)
    {
        if (characterAnimator != null) characterAnimator.SetTrigger(animationTrigger);
    }

    [ClientRpc]
    private void StopCheatAnimationClientRpc()
    {
        if (characterAnimator != null) characterAnimator.SetTrigger("CancelAction");
    }
}