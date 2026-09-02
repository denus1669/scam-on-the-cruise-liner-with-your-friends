using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Универсальный компонент для управления мухлежом.
/// Добавляется на префаб Бота ИЛИ префаб Игрока.
/// </summary>
public abstract class CheatController : NetworkBehaviour
{
    [Header("Настройки мухлежа")]
    [Tooltip("Список доступных видов мухлежа для этого бота.")]
    [SerializeField] protected List<CheatAction> availableCheats = new List<CheatAction>();

    [Header("Ссылки")]
    [SerializeField] protected CheatAction currentCheatAction;
    [SerializeField] protected Animator characterAnimator;


    protected IGameTable currentTable;
    protected Coroutine cheatCoroutine;

    // Сетевое состояние мухлежа
    protected NetworkVariable<bool> isCheating = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public bool IsCheating => isCheating.Value;

    // --- ГЛОБАЛЬНЫЕ СОБЫТИЯ ДЛЯ ВИЗУАЛА (срабатывают на всех клиентах) ---
    public event Action<string> OnCheatStarted;
    public event Action OnCheatCanceled;
    public event Action<bool> OnCheatingStateChanged;

    protected virtual void Awake()
    {
        characterAnimator = GetComponentInChildren<Animator>();
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

    public virtual bool CanCheat()
    {
        // Проверяем, что персонаж не в процессе мухлежа и что он владелец (или бот)
        return !isCheating.Value && IsOwner;
    }

    /// <summary>
    /// Пытается начать мухлеж. 
    /// Бот будет передавать specificCheat = null, чтобы выбрать случайный мухлеж.
    /// Игрок может передавать конкретный specificCheat, выбранный через UI.
    /// </summary>
    public virtual bool TryInitiateCheat(IGameTable table, CheatAction specificCheat = null)
    {
        if (!IsServer || isCheating.Value) return false;
        currentTable = table;
        CheatRoutine(specificCheat);

        return true;
    }

    protected virtual void CheatRoutine(CheatAction cheat)
    {
        isCheating.Value = true;
        currentCheatAction = cheat;
        // Оповещаем всех клиентов (запуск подозрительной анимации и звуков на клиентах)
        NotifyCheatStartedClientRpc(cheat.AnimationTriggerName);
    }


    protected void CompleteCheat(CheatAction cheat, string who)
    {
        Debug.Log($"[CheatController] Мухлеж '{cheat.CheatName}' успешен для !");
        cheat.ApplyCheatResult(currentTable, who);
        isCheating.Value = false;
        currentCheatAction = null;
    }

    protected virtual void CancelCheat()
    {
        if (cheatCoroutine != null) StopCoroutine(cheatCoroutine);
        isCheating.Value = false;
        currentCheatAction = null;
        StopCheatAnimationClientRpc();
    }

    /// <summary>
    /// Обработка последствий успешной поимки за руку.
    /// </summary>
    public virtual void HandleCheatCaught(ulong accuserClientId)
    {
        Debug.Log($"[CheatSystem] ПОЙМАН ЗА РУКУ!");

        // 1. Оповещаем всех клиентов (для UI-эффектов, звуков)
        GameInterruptedClientRpc(accuserClientId);
        ForceCloseMinigameClientRpc();

        // 2. Делегируем столу решение (форс-стоп, списание денег, уход бота)
        currentTable?.OnCheaterCaught(
            accuserClientId,
            isCheaterBot: this is BotCheatController
        );
        // 3. Сбрасываем состояние мухлежа
        CancelCheat();

        ExposureManager.Instance?.AddExposure();
    }

    [ClientRpc]
    protected void ForceCloseMinigameClientRpc(ClientRpcParams clientRpcParams = default)
    {
        // Для ботов это не нужно, но метод универсальный
        if (this is PlayerCheatController pcc)
        {
            pcc.ForceCloseActiveMinigame();
        }
    }

    [ClientRpc]
    protected virtual void GameInterruptedClientRpc(ulong accuserClientId)
    {
        Debug.Log($"[UI/FX] ИГРА ПРЕРВАНА!");
    }



    protected virtual void HandleCheatingStateChanged(bool previous, bool current)
    {
        OnCheatingStateChanged?.Invoke(current);
    }

    [ClientRpc]
    protected virtual void NotifyCheatStartedClientRpc(string animationTriggerName)
    {
    }

    [ClientRpc]
    protected virtual void StopCheatAnimationClientRpc()
    {
        if (characterAnimator != null) characterAnimator.SetTrigger("CancelAction");
    }
}