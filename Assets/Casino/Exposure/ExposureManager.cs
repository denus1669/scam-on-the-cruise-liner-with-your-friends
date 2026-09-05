using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ExposureManager : NetworkBehaviour
{
    public static ExposureManager Instance { get; private set; }

    [SerializeField] private int MAX_EXPOSURE = 3;

    [Header("Стадии раскрытия (индекс = level - 1)")]
    [SerializeField] private List<ExposureStage> stages = new();

    // Сетевая переменная: уровень раскрытия (0..3)
    private readonly NetworkVariable<int> _exposureLevel = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public int CurrentLevel => _exposureLevel.Value;

    /// <summary>Срабатывает на сервере И на всех клиентах при смене уровня.</summary>
    public event Action<int> OnExposureChanged;

    /// <summary>Срабатывает при достижении MAX_EXPOSURE (игра проиграна).</summary>
    public event Action OnGameOver;

    private void Awake()
    {
        // Синглтон — на сцене должен быть только один
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
        _exposureLevel.OnValueChanged += HandleLevelChanged;

        // Применяем текущее состояние (для клиентов, подключившихся позже)
        if (_exposureLevel.Value > 0)
            ApplyStage(_exposureLevel.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _exposureLevel.OnValueChanged -= HandleLevelChanged;
    }

    /// <summary>
    /// Вызывается сервером, когда инспектор ловит игрока на мухлеже.
    /// </summary>
    public void AddExposure()
    {
        if (!IsServer) return;
        if (_exposureLevel.Value >= MAX_EXPOSURE) return;

        int newLevel = _exposureLevel.Value + 1;
        _exposureLevel.Value = newLevel;

        Debug.Log($"[Exposure] Уровень раскрытия: {newLevel}/{MAX_EXPOSURE}");

        // Все изменения локации синхронизируются через OnValueChanged
        if (newLevel >= MAX_EXPOSURE)
        {
            TriggerGameOverClientRpc();
        }
    }

    private void HandleLevelChanged(int previousValue, int newValue)
    {
        ApplyStage(newValue);
        OnExposureChanged?.Invoke(newValue);
    }

    private void ApplyStage(int level)
    {
        if (level <= 0 || level > stages.Count)
            return;

        var stage = stages[level - 1];
        if (stage == null) return;

        // Активируем новые объекты (двери-сейфы, пулемёты, камеры)
        foreach (var go in stage.objectsToActivate)
        {
            if (go != null) go.SetActive(true);
        }

        // Деактивируем старые (обычная дверь)
        foreach (var go in stage.objectsToDeactivate)
        {
            if (go != null) go.SetActive(false);
        }

        stage.onStageApplied?.Invoke();

        Debug.Log($"[Exposure] Применена стадия {level}: активировано {stage.objectsToActivate.Count}, деактивировано {stage.objectsToDeactivate.Count}");
    }

    [ClientRpc]
    private void TriggerGameOverClientRpc()
    {
        Debug.Log("[Exposure] GAME OVER — игроки раскрыты!");
        OnGameOver?.Invoke();

        // Переключаем сессию на состояние проигрыша
        if (GameSessionManager.Instance != null)
            GameSessionManager.Instance.SetGameState(GameState.LoseGame);

        // Здесь можно запустить кат-сцену, показать UI "Вы провалились",
        // либо сервер через N секунд выкинет всех игроков.
    }
}