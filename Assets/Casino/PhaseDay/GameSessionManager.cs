using Blocks.Gameplay.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GameSessionManager : NetworkBehaviour
{
    public static GameSessionManager Instance { get; private set; }
    public static event Action OnInstanceReady;
    public static event Action OnInstanceDestroyed;

    [SerializeField] private DayConfiguration dayConfiguration;
    [SerializeField] private SlotMachineBreakdownManager slotMachineBreakdownManager;
    [SerializeField] private CasinoBank casinoBank;
    [SerializeField] private BotSpawner botSpawner;
    [SerializeField] private GameTable[] gameTables;

    private readonly NetworkVariable<GameState> _gameState = new(GameState.Preparing,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _currentDay = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> _timeRemaining = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public GameState CurrentState => _gameState.Value;
    public int CurrentDay => _currentDay.Value;
    public float TimeRemaining => _timeRemaining.Value;
    public int TotalDays => dayConfiguration.daysCount;
    public bool IsLastDay => _currentDay.Value >= dayConfiguration.daysCount;
    public bool IsSessionWon => false; // TODO: условие победы (например, по прибыли)

    // API для состояний
    /// <summary>Касса казино (для состояний и будущей статистики).</summary>
    public CasinoBank Bank => casinoBank;
    public DayConfiguration DayConfiguration => dayConfiguration;
    public BotSpawner BotSpawner => botSpawner;
    public SlotMachineBreakdownManager BreakdownManager => slotMachineBreakdownManager;

    private Dictionary<GameState, GameStateBase> _states;
    private GameStateBase _active;

    public event Action<GameState> OnStateChanged;
    public event Action<int> OnDayStarted;
    public event Action<float> OnTimerTick;
    public event Action<int> OnDayEnded;
    public event Action OnSessionEnded;

    private void Awake()
    {
        ValidateReferences();

        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        OnInstanceReady?.Invoke();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _gameState.OnValueChanged += (previous, current) => OnStateChanged?.Invoke(current);
        _currentDay.OnValueChanged += (previous, current) => { if (current > previous) OnDayStarted?.Invoke(current); };
        _timeRemaining.OnValueChanged += (previous, current) => OnTimerTick?.Invoke(current);

        if (!IsServer) return; // клиенты только читают состояние для UI

        _states = new GameStateBase[]
        {
            new PreparingState(this), new DayActiveState(this), new EndDayState(this),
            new LoseGameState(this), new WinGameState(this), new GameStatisticState(this),
        }.ToDictionary(s => s.Type);

        _active = _states[_gameState.Value];
        _active.Enter();
    }

    private void Update()
    {
        if (!IsServer || _active == null) return;
        _active.Tick(Time.deltaTime);
    }

    public void SetGameState(GameState next)
    {
        if (!IsServer) return;
        Debug.Log($"[GameSessionManager] {_gameState.Value} → {next}");
        _active?.Exit();

        _gameState.Value = next;
        _active = _states[next];
        _active.Enter();
    }

    // ---------- RPC: внешний API не меняется, DoorInteractable и UI не трогаем ----------
    /// <summary>
    /// Запускает сессию из лобби. Вызывается из LobbyInteractable.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void StartSessionServerRpc() => _active?.OnStartSessionRequested();

    [Rpc(SendTo.Server)]
    public void StartDayServerRpc() => _active?.OnStartDayRequested();

    [Rpc(SendTo.Server)]
    public void RequestContinueServerRpc() => _active?.OnContinuePressed();

    // ---------- Операции, которые состояния вызывают через контекст ----------
    [Rpc(SendTo.Server)]

    public void SetTimeRemainingServerRpc(float v)
    {
        if (!IsServer) return;
        SetTimeRemaining(v);
    }
    public void SetTimeRemaining(float v)
    {
        if (!IsServer)
        {
            Debug.Log($"[GameSessionManager] SetTimeRemaining !IsServer");
            return;
        }
        _timeRemaining.Value = v;
    }

    public void AdvanceDay() => _currentDay.Value++;

    public void ForceStopAllGames()
    {
        foreach (var table in gameTables)
        {
            if (table == null) continue; // <-- защита от уничтоженных столов
            if (table.IsGameStarted)
            {
                table.ForceStopGame(ulong.MaxValue, false, "DayEnded");
            }
        }
    }

    public void SendBotsToExit()
    {
        if (botSpawner == null) return;

        var bots = botSpawner.GetSpawnedBots();
        foreach (var bot in bots)
        {
            // Проверяем, жив ли ещё объект в Unity
            if (bot == null) continue;

            if (bot.TryGetComponent<BotAgent>(out var botAgent))
            {
                botAgent.GoToExit();
            }
        }
    }

    /// <summary>
    /// Условие проигрыша: баланс кассы ниже порога текущего дня.
    /// </summary>
    public bool CheckLoseCondition()
    {
        if (!IsServer) return false;
        if (casinoBank == null || dayConfiguration == null) return false;

        int threshold = dayConfiguration.GetChipThresholdForDay(_currentDay.Value);
        return casinoBank.CurrentBalance < threshold;
    }

    public void NotifyDayEnded(int day) => NotifyDayEndedClientRpc(day);
    public void NotifySessionEnded() => NotifySessionEndedClientRpc();

    [ClientRpc] private void NotifyDayEndedClientRpc(int day) => OnDayEnded?.Invoke(day);
    [ClientRpc] private void NotifySessionEndedClientRpc() => OnSessionEnded?.Invoke();


    private void ValidateReferences()
    {
        if (dayConfiguration == null)
            Debug.LogError($"{name}: dayConfiguration is not assigned!");

        if (slotMachineBreakdownManager == null)
            Debug.LogError($"{name}: slotMachineBreakdownManager is not assigned!");

        if (casinoBank == null)
            Debug.LogError($"{name}: casinoBank is not assigned!");

        if (botSpawner == null)
            Debug.LogError($"{name}: botSpawner is not assigned!");

        if (gameTables == null || gameTables.Length == 0)
            Debug.LogError($"{name}: gameTables is not assigned or empty!");
        else
        {
            for (int i = 0; i < gameTables.Length; i++)
            {
                if (gameTables[i] == null)
                    Debug.LogError($"{name}: gameTables[{i}] is not assigned!");
            }
        }
    }
}