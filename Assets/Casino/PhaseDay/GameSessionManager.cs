using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using static GameSessionManager;

/// <summary>
/// Дирижёр игровой сессии: управляет фазами, днями и таймерами.
/// Синглтон на сцене. Все решения принимаются только на сервере.
/// </summary>
public class GameSessionManager : NetworkBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    [Header("Ссылки")]
    [SerializeField] private DayConfiguration dayConfiguration;
    [SerializeField] private BotSpawner botSpawner;
    [SerializeField] private GameTable[] gameTables; // Массив всех столов в казино


    // ---------- Состояние сессии ----------
    public enum SessionPhase
    {
        Preparation,     // Подготовка
        GamePhase,       // Идёт игра, таймер тикает
        DayEnding,       // Боты уходят, столы останавливаются
        DayStatistics,   // Экран статистики дня
        SessionEnded     // Финальная статистика за все дни
    }

    private readonly NetworkVariable<SessionPhase> _currentPhase = new(
        SessionPhase.Preparation,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<int> _currentDay = new(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly NetworkVariable<float> _timeRemaining = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Публичные свойства для чтения клиентами
    public SessionPhase CurrentPhase => _currentPhase.Value;
    public int CurrentDay => _currentDay.Value;
    public float TimeRemaining => _timeRemaining.Value;
    public int TotalDays => dayConfiguration != null ? dayConfiguration.daysCount : 0;

    // Coroutine для таймера (только на сервере)
    private Coroutine _gamePhaseCoroutine;

    // Локальное время для Update (только сервер)
    private float _lastTimerUpdate;

    // Счётчик игроков, нажавших "Продолжить" в DayStatistics
    private int _continuePressesCount = 0;

    // ---------- События для UI ----------
    public event Action<SessionPhase> OnPhaseChanged;
    public event Action<int> OnDayStarted;
    public event Action<float> OnTimerTick;
    public event Action<int> OnDayEnded;  // передаёт номер завершённого дня
    public event Action OnSessionEnded;

    // ---------- Unity / NetworkBehaviour ----------
    private void Awake()
    {
        // Простой синглтон (не-сетевой, т.к. объект на сцене)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Подписка на изменения фазы для излучения локальных событий
        _currentPhase.OnValueChanged += HandlePhaseChanged;
        _currentDay.OnValueChanged += HandleDayChanged;
        _timeRemaining.OnValueChanged += HandleTimeChanged;

        // Излучаем начальное состояние для поздних подписчиков (например, UI)
        OnPhaseChanged?.Invoke(_currentPhase.Value);
        OnDayStarted?.Invoke(_currentDay.Value);
    }

    public override void OnNetworkDespawn()
    {
        _currentPhase.OnValueChanged -= HandlePhaseChanged;
        _currentDay.OnValueChanged -= HandleDayChanged;
        _timeRemaining.OnValueChanged -= HandleTimeChanged;

        if (Instance == this)
            Instance = null;

        base.OnNetworkDespawn();
    }

    // ---------- Update для таймера (только сервер) ----------
    private void Update()
    {
        if (!IsServer) return;
        if (_currentPhase.Value != SessionPhase.GamePhase) return;

        // Обновляем таймер раз в секунду
        if (Time.time - _lastTimerUpdate >= 1f)
        {
            _lastTimerUpdate = Time.time;
            _timeRemaining.Value = Mathf.Max(0, _timeRemaining.Value - 1f);

            if (_timeRemaining.Value <= 0)
            {
                EndDay();
            }
        }
    }


    // ---------- Обработчики изменений NetworkVariable ----------
    private void HandlePhaseChanged(SessionPhase previous, SessionPhase current)
    {
        OnPhaseChanged?.Invoke(current);
        Debug.Log($"[GameSessionManager] Фаза: {previous} → {current}");
    }

    private void HandleDayChanged(int previous, int current)
    {
        // Излучаем только при старте нового дня (когда day > previous)
        if (current > previous)
            OnDayStarted?.Invoke(current);
    }

    private void HandleTimeChanged(float previous, float current)
    {
        OnTimerTick?.Invoke(current);
    }

    // ---------- Управление фазами (только сервер) ----------

    /// <summary>
    /// Запускает игровой день. Вызывается из DoorInteractable.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void StartDayServerRpc()
    {
        if (!IsServer) return;
        if (_currentPhase.Value != SessionPhase.Preparation)
        {
            Debug.LogWarning($"[GameSessionManager] Нельзя начать день в фазе {_currentPhase.Value}");
            return;
        }
        Debug.Log($"[GameSessionManager] Игрок инициировал старт дня {_currentDay.Value}");
        StartCoroutine(StartDayRoutine());
    }

    private IEnumerator StartDayRoutine()
    {
        Debug.Log($"[GameSessionManager] === ДЕНЬ {_currentDay.Value} НАЧИНАЕТСЯ ===");

        // Спавним ботов
        SpawnBotsForDay();

        // Переход в GamePhase
        _currentPhase.Value = SessionPhase.GamePhase;
        _timeRemaining.Value = dayConfiguration.gamePhaseDuration;
        _lastTimerUpdate = Time.time;

        yield break;
    }
    private void SpawnBotsForDay()
    {
        if (botSpawner == null)
        {
            Debug.LogError("[GameSessionManager] botSpawner не назначен!");
            return;
        }

        for (int i = 0; i < dayConfiguration.botCount; i++)
        {
            botSpawner.SpawnBot(i);
        }

        Debug.Log($"[GameSessionManager] Заспавнено {dayConfiguration.botCount} ботов");
    }

    /// <summary>
    /// Завершает текущий игровой день.
    /// </summary>
    private void EndDay()
    {
        if (!IsServer) return;
        if (_currentPhase.Value != SessionPhase.GamePhase) return;

        if (_gamePhaseCoroutine != null)
        {
            StopCoroutine(_gamePhaseCoroutine);
            _gamePhaseCoroutine = null;
        }

        _currentPhase.Value = SessionPhase.DayEnding;
        Debug.Log($"[GameSessionManager] День {_currentDay.Value} завершается...");

        // 1. Принудительно останавливаем все игры на столах
        ForceStopAllGames();

        // 2. Командуем ботам идти к выходу
        SendBotsToExit();

        // Для MVP: просто через 5 секунд переход в DayStatistics
        StartCoroutine(TransitionToDayStatsRoutine());
    }

    private void ForceStopAllGames()
    {
        if (gameTables == null || gameTables.Length == 0)
        {
            Debug.LogWarning("[GameSessionManager] Массив столов пуст!");
            return;
        }

        foreach (var table in gameTables)
        {
            if (table != null && table.IsGameStarted)
            {
                // winnerClientId = ulong.MaxValue (никто не выиграл)
                // isCheaterBot = false (это не поимка читера, просто конец дня)
                // reason = "DayEnded"
                table.ForceStopGame(ulong.MaxValue, false, "DayEnded");
                Debug.Log($"[GameSessionManager] Игра на столе {table.name} принудительно остановлена");
            }
        }
    }

    private void SendBotsToExit()
    {
        if (botSpawner == null) return;

        var bots = botSpawner.GetSpawnedBots();
        foreach (var bot in bots)
        {
            if (bot == null) continue;

            var botAgent = bot.GetComponent<BotAgent>();
            if (botAgent != null)
            {
                botAgent.GoToExit();
                Debug.Log($"[GameSessionManager] Бот {bot.name} отправлен к выходу");
            }
        }
    }

    private IEnumerator TransitionToDayStatsRoutine()
    {
        yield return new WaitForSeconds(5f);

        int finishedDay = _currentDay.Value;
        _currentPhase.Value = SessionPhase.DayStatistics;
        _continuePressesCount = 0;

        // Уведомляем клиентов для показа статистики
        NotifyDayEndedClientRpc(finishedDay);
    }

    /// <summary>
    /// Вызывается игроками в DayStatistics при нажатии "Продолжить".
    /// </summary>
    [Rpc(SendTo.Server)]
    public void RequestContinueServerRpc()
    {
        if (!IsServer) return;
        if (_currentPhase.Value != SessionPhase.DayStatistics) return;

        _continuePressesCount++;
        Debug.Log($"[GameSessionManager] Игрок нажал 'Продолжить'. Всего: {_continuePressesCount}");

        // TODO: Здесь нужно проверять количество игроков на сервере
        // Для MVP: переход сразу после первого нажатия
        if (_continuePressesCount >= 1)
        {
            StartCoroutine(TransitionToNextDayRoutine());
        }
    }

    private IEnumerator TransitionToNextDayRoutine()
    {
        yield return new WaitForSeconds(2f); // Короткая пауза для UX

        if (_currentDay.Value >= dayConfiguration.daysCount)
        {
            // Сессия завершена
            _currentPhase.Value = SessionPhase.SessionEnded;
            NotifySessionEndedClientRpc();
        }
        else
        {
            // Переход к следующему дню
            _currentDay.Value++;
            _currentPhase.Value = SessionPhase.Preparation;
        }
    }

    // ---------- ClientRpc для уведомлений ----------

    [ClientRpc]
    private void NotifyDayEndedClientRpc(int finishedDay)
    {
        OnDayEnded?.Invoke(finishedDay);
    }

    [ClientRpc]
    private void NotifySessionEndedClientRpc()
    {
        OnSessionEnded?.Invoke();
    }
}