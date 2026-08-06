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
    [SerializeField] private BotSpawner botSpawner;
    [SerializeField] private GameTable[] gameTables;

    private readonly NetworkVariable<GameState> _gameState = new(GameState.Preparing,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<int> _currentDay = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> _timeRemaining = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public GameState CurrentState => _gameState.Value;
    public int CurrentDay => _currentDay.Value;
    public float TimeRemaining => _timeRemaining.Value;
    public bool IsLastDay => _currentDay.Value >= dayConfiguration.daysCount;
    public bool IsSessionWon => false; // TODO: условие победы (например, по прибыли)

    // API для состояний
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
        _active?.Exit();
        Debug.Log($"[GameSessionManager] {_gameState.Value} → {next}");
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
    public void SetTimeRemaining(float v) => _timeRemaining.Value = v;
    public void AdvanceDay() => _currentDay.Value++;

    public void ForceStopAllGames()
    {
        foreach (var table in gameTables)
            if (table != null && table.IsGameStarted)
                table.ForceStopGame(ulong.MaxValue, false, "DayEnded");
    }

    public void SendBotsToExit()
    {
        foreach (var bot in botSpawner.GetSpawnedBots())
            bot?.GetComponent<BotAgent>()?.GoToExit();
    }

    public void NotifyDayEnded(int day) => NotifyDayEndedClientRpc(day);
    public void NotifySessionEnded() => NotifySessionEndedClientRpc();

    [ClientRpc] private void NotifyDayEndedClientRpc(int day) => OnDayEnded?.Invoke(day);
    [ClientRpc] private void NotifySessionEndedClientRpc() => OnSessionEnded?.Invoke();
}