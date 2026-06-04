// Casino/Scripts/Runtime/Tables/BlackjackTable.cs
using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Сетевой контроллер стола Блекджек (Крупье vs Бот).
/// Вся логика фаз, подозрения, таймеров и валидации мухлежа исполняется строго на хосте.
/// </summary>
public class BlackjackTable : NetworkBehaviour, IBlackjackTable
{
    [SerializeField] private BlackjackConfig _config;
    [SerializeField] private Transform _dealerSeatAnchor;

    // NetworkVariables (приватные, синхронизируются хостом)
    private readonly NetworkVariable<int> _currentPhase = new NetworkVariable<int>((int)BlackjackRoundPhase.Idle);
    private readonly NetworkVariable<float> _suspicion = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<ulong> _dealerOwnerId = new NetworkVariable<ulong>();
    private readonly NetworkVariable<bool> _cheatWindowActive = new NetworkVariable<bool>(false);

    // Публичные события для UI и контроллеров
    public event Action<BlackjackRoundPhase> OnPhaseChanged;
    public event Action<float> OnSuspicionChanged;
    public event Action OnTableStateChanged;

    // Read-only свойства
    public BlackjackRoundPhase CurrentPhase => (BlackjackRoundPhase)_currentPhase.Value;
    public float CurrentSuspicion => _suspicion.Value;
    public ulong DealerOwnerId => _dealerOwnerId.Value;
    public bool IsCheatWindowActive => _cheatWindowActive.Value;

    // Server-only данные
    private float _phaseTimer;
    private bool _roundActive;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _currentPhase.OnValueChanged += OnPhaseChangedServer;
        }

        // Проброс NetworkVariable в события
        _currentPhase.OnValueChanged += (oldVal, newVal) => OnPhaseChanged?.Invoke((BlackjackRoundPhase)newVal);
        _suspicion.OnValueChanged += (oldVal, newVal) => OnSuspicionChanged?.Invoke(newVal);
        _cheatWindowActive.OnValueChanged += (oldVal, newVal) => OnTableStateChanged?.Invoke();
    }

    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        if (!_roundActive)
        {
            return;
        }

        _phaseTimer -= Time.deltaTime;

        // Естественное затухание подозрения вне окна мухлежа
        if (!_cheatWindowActive.Value && _suspicion.Value > 0f)
        {
            _suspicion.Value = Mathf.Max(0f, _suspicion.Value - _config.suspicionDecayRate * Time.deltaTime);
        }

        if (_phaseTimer <= 0f)
        {
            AdvancePhaseServer();
        }
    }

    /// <summary>
    /// Запрос на занятие места крупье. Валидируется хостом.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AssignDealerServerRpc(ulong clientId)
    {
        if (!IsServer || _dealerOwnerId.Value != 0)
        {
            return;
        }

        float distance = Vector3.Distance(GetClientPosition(clientId), _dealerSeatAnchor.position);
        if (distance > _config.interactionDistance)
        {
            return;
        }

        _dealerOwnerId.Value = clientId;
        StartRoundLoopServer();
    }

    /// <summary>
    /// Запрос на покидание стола. Сбрасывает раунд.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void LeaveTableServerRpc(ulong clientId)
    {
        if (!IsServer || _dealerOwnerId.Value != clientId)
        {
            return;
        }

        _dealerOwnerId.Value = 0;
        AbortRoundServer();
    }

    /// <summary>
    /// Запрос на мухлеж. Открывает окно и разрешает клиенту запустить мини-игру.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestCheatServerRpc(ulong clientId, DealerCheatType cheatType)
    {
        if (!IsServer || _dealerOwnerId.Value != clientId)
        {
            return;
        }

        if (CurrentPhase != BlackjackRoundPhase.CheatWindow || !_cheatWindowActive.Value)
        {
            return;
        }

        // Хост разрешает клиенту запустить мини-игру.
        // Фактическая валидация произойдёт в SubmitCheatResultServerRpc.
        // Здесь можно добавить ClientRpc для запуска UI мини-игры, если требуется.
    }

    /// <summary>
    /// Приём результата мини-игры мухлежа. Строгая валидация на хосте.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SubmitCheatResultServerRpc(ulong clientId, DealerCheatType cheatType, bool success)
    {
        if (!IsServer || _dealerOwnerId.Value != clientId)
        {
            return;
        }

        if (CurrentPhase != BlackjackRoundPhase.CheatWindow)
        {
            return;
        }

        float suspicionDelta = success ? _config.suspicionGrowthOnSuccess : _config.suspicionGrowthOnFail;
        _suspicion.Value = Mathf.Min(_config.maxSuspicion, _suspicion.Value + suspicionDelta);

        if (_suspicion.Value >= _config.maxSuspicion)
        {
            // Провал дня: бот заметил мухлеж
            TriggerFailServer();
            return;
        }

        if (success)
        {
            // Успешный мухлеж: прогресс цели дня (интеграция с DayManager)
            // DayManager.Instance.RecordSuccessfulCheatServerRpc();
        }

        // Закрываем окно мухлежа после попытки
        _cheatWindowActive.Value = false;
        AdvancePhaseServer();
    }

    // ==================== SERVER LOOP ====================

    private void StartRoundLoopServer()
    {
        _roundActive = true;
        _currentPhase.Value = (int)BlackjackRoundPhase.Dealing;
        _phaseTimer = _config.dealingPhaseDuration;
        _suspicion.Value = 0f;
        _cheatWindowActive.Value = false;
    }

    private void AdvancePhaseServer()
    {
        BlackjackRoundPhase nextPhase = CurrentPhase switch
        {
            BlackjackRoundPhase.Dealing => BlackjackRoundPhase.CheatWindow,
            BlackjackRoundPhase.CheatWindow => BlackjackRoundPhase.Resolving,
            BlackjackRoundPhase.Resolving => BlackjackRoundPhase.RoundEnd,
            BlackjackRoundPhase.RoundEnd => BlackjackRoundPhase.Dealing,
            _ => BlackjackRoundPhase.Idle
        };

        _currentPhase.Value = (int)nextPhase;

        switch (nextPhase)
        {
            case BlackjackRoundPhase.CheatWindow:
                _cheatWindowActive.Value = true;
                _phaseTimer = _config.cheatWindowDuration;
                break;
            case BlackjackRoundPhase.Resolving:
                _cheatWindowActive.Value = false;
                _phaseTimer = _config.resolvePhaseDuration;
                break;
            case BlackjackRoundPhase.RoundEnd:
                _phaseTimer = 2f;
                break;
            case BlackjackRoundPhase.Dealing:
                _phaseTimer = _config.dealingPhaseDuration;
                break;
        }
    }

    private void AbortRoundServer()
    {
        _roundActive = false;
        _currentPhase.Value = (int)BlackjackRoundPhase.Idle;
        _cheatWindowActive.Value = false;
        _suspicion.Value = 0f;
        _phaseTimer = 0f;
    }

    private void TriggerFailServer()
    {
        _cheatWindowActive.Value = false;
        _currentPhase.Value = (int)BlackjackRoundPhase.Resolving;
        // Интеграция с DayManager: RecordFailServerRpc(FailReason.CaughtCheating)
        AbortRoundServer();
    }

    private void OnPhaseChangedServer(int oldPhase, int newPhase)
    {
        // Хост-триггеры: звуки раздачи, ClientRpc для анимаций карт, логирование
    }

    private Vector3 GetClientPosition(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client) &&
            client.PlayerObject != null)
        {
            return client.PlayerObject.transform.position;
        }

        return Vector3.zero;
    }
}