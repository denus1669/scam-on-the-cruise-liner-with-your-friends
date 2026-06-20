
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

    private void Awake()
    {
        characterAnimator = GetComponentInChildren<Animator>();

        // Если на этом же объекте есть BotAgent, значит этот контроллер принадлежит боту.
        // Иначе - это контроллер реального игрока.
        isBot = TryGetComponent<BotAgent>(out botAgent);
        displeasureController = GetComponent<BotDispleasureController>();
    }

    /// <summary>
    /// Пытается начать мухлеж. 
    /// Бот будет передавать specificCheat = null, чтобы выбрать случайный мухлеж.
    /// Игрок может передавать конкретный specificCheat, выбранный через UI.
    /// </summary>
    public bool TryInitiateCheat(IGameTable table, CheatAction specificCheat = null)
    {
        if (!IsServer || isCheating.Value) return false;

        // Оповещаем компонент недовольства о том, что бот делает подозрительное движение
        if (isBot && displeasureController != null)
        {
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

    private IEnumerator CheatRoutine(CheatAction cheat, CheatContext context)
    {
        isCheating.Value = true;
        currentCheatAction = cheat;

        string actorName = isBot ? $"Бот {gameObject.name}" : $"Игрок {OwnerClientId}";
        Debug.Log($"[CheatController] {actorName} начинает мухлевать: {cheat.CheatName}");

        // Проигрываем анимацию у всех клиентов
        PlayCheatAnimationClientRpc(cheat.AnimationTriggerName);

        // Ждем окно времени, за которое мухлевщика можно "поймать за руку"
        float timer = cheat.AnimationDuration;
        while (timer > 0)
        {
            timer -= Time.deltaTime;

            // Защита: если игра внезапно закончилась, прерываем мухлеж
            if (context.Table == null || !context.Table.IsGameStarted)
            {
                CancelCheat();
                yield break;
            }
            yield return null;
        }

        // Если никто не нажал "Обвинить", мухлеж удался!
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
        StopCheatAnimationClientRpc();
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

    /// <summary>
    /// Обработка последствий успешной поимки за руку.
    /// </summary>
    private void HandleCheatCaught(ulong accuserClientId)
    {
        if (activeContext == null || activeContext.Table == null) return;

        string cheaterName = isBot ? $"Бот {gameObject.name}" : $"Игрок {OwnerClientId}";
        Debug.Log($"[CheatSystem] {cheaterName} ПОЙМАН ЗА РУКУ! Остановка игры...");

        // 1. Оповещаем всех клиентов (для UI и визуальных эффектов)
        GameInterruptedClientRpc(accuserClientId, isBot);

        // 2. Останавливаем игру на столе
        // Если жульничал бот -> побеждает игрок (accuserClientId).
        // Если жульничал игрок -> побеждает казино/бот (указываем ulong.MaxValue).
        ulong winnerId = isBot ? accuserClientId : ulong.MaxValue;
        activeContext.Table.ForceStopGame(winnerId, isCheaterBot: isBot, reason: "Cheating");

        // 3. Логика штрафов (списание токенов, анимации реакции)
        if (isBot)
        {
            if (botAgent != null)
            {
                // Заставляем бота обиженно уйти из-за стола к выходу
                botAgent.GoToExit();
            }
        }
        else
        {
            // Логика штрафа для реального игрока (списание денег, понижение репутации и т.д.)
        }
    }

    [ClientRpc]
    private void GameInterruptedClientRpc(ulong accuserClientId, bool cheaterIsBot)
    {
        string cheaterName = cheaterIsBot ? $"Бот {gameObject.name}" : $"Игрок {OwnerClientId}";
        Debug.Log($"[UI/FX] ИГРА ПРЕРВАНА! Игрок {accuserClientId} поймал за руку: {cheaterName}!");

        // TODO: Вызвать событие для UI, чтобы показать большую надпись "ПОЙМАН ЗА РУКУ!"
        // Например: UIManager.Instance.ShowCheatCaughtMessage(cheaterName, accuserClientId);
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
            Debug.Log($"[CheatSystem] Игрок {accuserClientId} поймал за руку: {actorName}!");

            // Вызываем последствия ДО CancelCheat, чтобы activeContext еще был доступен
            HandleCheatCaught(accuserClientId);

            CancelCheat();
        }
        else
        {
            Debug.Log($"[CheatSystem] Игрок {accuserClientId} ложно обвинил: {actorName}.");
            // TODO: Ложное обвинение (рост подозрительности, штраф обвинителю)
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

        int randomValue = Random.Range(0, totalWeight);
        foreach (var cheat in validCheats)
        {
            randomValue -= cheat.SelectionWeight;
            if (randomValue < 0) return cheat;
        }

        return validCheats[validCheats.Count - 1];
    }
}