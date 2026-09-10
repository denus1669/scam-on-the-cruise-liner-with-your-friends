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

    // Локальное состояние мухлежа (на сервере - авторитетное, на клиенте - визуальная копия)
    protected bool isCheatingLocal;
    public bool IsCheating => isCheatingLocal;

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
        // При спавне синхронизируем начальное состояние для новых клиентов
        if (IsServer && isCheatingLocal)
        {
            SyncCheatingStateClientRpc(true);
        }
        OnCheatingStateChanged?.Invoke(isCheatingLocal);
    }

    public virtual bool CanCheat()
    {
        // Проверяем, что персонаж не в процессе мухлежа и что он владелец (или бот)
        return !isCheatingLocal && IsOwner;
    }

    /// <summary>
    /// Пытается начать мухлеж. 
    /// Бот будет передавать specificCheat = null, чтобы выбрать случайный мухлеж.
    /// Игрок может передавать конкретный specificCheat, выбранный через UI.
    /// </summary>
    public virtual bool TryInitiateCheat(IGameTable table, CheatAction specificCheat = null)
    {
        if (!IsServer || isCheatingLocal || !IsOwner) return false;
        currentTable = table;
        CheatRoutine(specificCheat);
        return true;
    }

    protected virtual void CheatRoutine(CheatAction cheat)
    {
        SetCheating(true);
        currentCheatAction = cheat;

        // Оповещаем всех клиентов (запуск подозрительной анимации и звуков на клиентах)
        NotifyCheatStartedClientRpc(cheat.AnimationTriggerName);
    }

    /// <summary>
    /// Единая точка изменения состояния читерства.
    /// На сервере меняет локально и рассылает обновления.
    /// На клиенте отправляет запрос на сервер.
    /// </summary>
    protected void SetCheating(bool value)
    {
        if (IsServer)
        {
            if (isCheatingLocal == value) return;

            isCheatingLocal = value;
            OnCheatingStateChanged?.Invoke(value);
            SyncCheatingStateClientRpc(value);
        }
        else
        {
            // Клиент запрашивает изменение у сервера
            SetCheatingServerRpc(value);
        }
    }

    [Rpc(SendTo.Server)]
    private void SetCheatingServerRpc(bool value)
    {
        // Валидация: принимаем изменения только от владельца объекта или если это бот (сервер управляет ботами сам, но на всякий случай)
        if (!IsOwner && !(this is BotCheatController))
        {
            Debug.LogWarning($"[Cheat] Попытка смены состояния от не-владельца! Sender: {NetworkManager.Singleton.LocalClientId}");
            return;
        }

        if (isCheatingLocal == value) return;

        isCheatingLocal = value;
        OnCheatingStateChanged?.Invoke(value);

        // Рассылаем подтверждение всем клиентам (включая отправителя)
        SyncCheatingStateClientRpc(value);
    }

    [ClientRpc]
    private void SyncCheatingStateClientRpc(bool value)
    {
        // На хосте уже установлено в SetCheating, но для чистоты можно оставить или добавить guard
        if (IsHost) return;

        isCheatingLocal = value;
        OnCheatingStateChanged?.Invoke(value);
    }

    protected void CompleteCheat(CheatAction cheat, string who)
    {
        Debug.Log($"[CheatController] Мухлеж '{cheat.CheatName}' успешен для {who}!");
        cheat.ApplyCheatResult(currentTable, who);
        SetCheating(false);
        currentCheatAction = null;
    }

    protected virtual void CancelCheat()
    {
        if (cheatCoroutine != null) StopCoroutine(cheatCoroutine);
        SetCheating(false);
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

    [ClientRpc]
    protected virtual void NotifyCheatStartedClientRpc(string animationTriggerName)
    {
        OnCheatStarted?.Invoke(animationTriggerName);
    }

    [ClientRpc]
    protected virtual void StopCheatAnimationClientRpc()
    {
        OnCheatCanceled?.Invoke();
        if (characterAnimator != null) characterAnimator.SetTrigger("CancelAction");
    }
}