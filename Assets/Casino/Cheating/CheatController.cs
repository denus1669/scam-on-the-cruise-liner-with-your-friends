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

    [Header("Ссыкла на банк казино")]
    [SerializeField] private CasinoBank casinoBank;

    // Сетевое состояние мухлежа
    private NetworkVariable<bool> isCheating = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private CheatAction currentCheatAction;
    private Coroutine cheatCoroutine;

    // Ссылки на компоненты
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



    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (casinoBank == null && IsServer)
        {
            casinoBank = FindAnyObjectByType<CasinoBank>();
        }

        // НОВОЕ: подписка на изменение состояния мухлежа
        isCheating.OnValueChanged += HandleCheatingStateChanged;

        // НОВОЕ: принудительное излучение текущего состояния для поздних подписчиков
        OnCheatingStateChanged?.Invoke(isCheating.Value);

    }

    private void Awake()
    {
        // Убрали привязку к Animator! Теперь код чистый.

        isBot = TryGetComponent<BotAgent>(out botAgent);
        displeasureController = GetComponent<BotDispleasureController>();
    }

    public bool TryInitiateCheat(IGameTable table, CheatAction specificCheat = null)
    {
        if (!IsServer || isCheating.Value) return false;

        // Оповещаем компонент недовольства о том, что бот делает подозрительное движение
        if (isBot && displeasureController != null)
        {
            // Здесь может быть вызов displeasureController
        }

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
                return false;
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

    private IEnumerator CheatRoutine(CheatAction cheat, CheatContext context)
    {
        isCheating.Value = true;
        currentCheatAction = cheat;

        string actorName = isBot ? $"Бот {gameObject.name}" : $"Игрок {OwnerClientId}";
        Debug.Log($"[CheatController] {actorName} начинает мухлевать: {cheat.CheatName}");

        // Вместо прямого вызова аниматора, рассылаем событие всем клиентам
        NotifyCheatStartedClientRpc(cheat.AnimationTriggerName);

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

        Debug.Log($"[CheatController] Мухлеж '{cheat.CheatName}' успешен для {actorName}!");
        cheat.ApplyCheatResult(context);

        isCheating.Value = false;
        currentCheatAction = null;
        activeContext = null;
    }

    private void CancelCheat()
    {
        if (cheatCoroutine != null) StopCoroutine(cheatCoroutine);
        isCheating.Value = false;
        currentCheatAction = null;
        activeContext = null;

        NotifyCheatCanceledClientRpc();
    }

    // --- ОБНОВЛЕННЫЕ СЕТЕВЫЕ МЕТОДЫ (Только рассылка событий) ---

    [ClientRpc]
    private void NotifyCheatStartedClientRpc(string animationTrigger)
    {
        // Вызываем событие на клиенте. Подписчики (например, аудио или аниматор) отреагируют сами.
        OnCheatStarted?.Invoke(animationTrigger);
    }

    [ClientRpc]
    private void NotifyCheatCanceledClientRpc()
    {
        OnCheatCanceled?.Invoke();
    }

    public void HandleCheatCaught(ulong accuserClientId)
    {
        if (activeContext == null || activeContext.Table == null) return;

        string cheaterName = isBot ? $"Бот {gameObject.name}" : $"Игрок {OwnerClientId}";
        Debug.Log($"[CheatSystem] {cheaterName} ПОЙМАН ЗА РУКУ! Делегируем обработку столу.");

        // 1. VFX для всех клиентов
        GameInterruptedClientRpc(accuserClientId, isBot);

        // 2. Делегируем столу решение: форс-стоп, откат руки, штраф и т.д.
        activeContext.Table.OnCheaterCaught(accuserClientId, isBot);

        // 3. Отменяем сам мухлёж (анимация, состояние)
        CancelCheat();

        // Больше никаких GoToExit и casinoBank.TryDeposit — 
        // это теперь ответственность стола через OnCheaterCaught.
    }

    [ClientRpc]
    private void GameInterruptedClientRpc(ulong accuserClientId, bool cheaterIsBot)
    {
        string cheaterName = cheaterIsBot ? $"Бот {gameObject.name}" : $"Игрок {OwnerClientId}";
        Debug.Log($"[UI/FX] ИГРА ПРЕРВАНА! Игрок {accuserClientId} поймал за руку: {cheaterName}!");
    }

    [Rpc(SendTo.Server)]
    public void AccuseServerRpc(ulong accuserClientId)
    {
        if (!IsServer) return;

        string actorName = isBot ? "Бот" : $"Игрок {OwnerClientId}";

        if (isCheating.Value)
        {
            Debug.Log($"[CheatSystem] Игрок {accuserClientId} поймал за руку: {actorName}!");
            HandleCheatCaught(accuserClientId);
            CancelCheat();
        }
        else
        {
            Debug.Log($"[CheatSystem] Игрок {accuserClientId} ложно обвинил: {actorName}.");
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
    private void HandleCheatingStateChanged(bool previous, bool current)
    {
        OnCheatingStateChanged?.Invoke(current);
    }
}