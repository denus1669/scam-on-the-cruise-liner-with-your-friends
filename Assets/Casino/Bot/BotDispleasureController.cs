using Blocks.Gameplay.Core;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Управляет раздражением бота. Работает на сервере в методе Update.
/// Поддерживает интерфейс IAccusable для обработки обвинений в мухлеже.
/// </summary>
[RequireComponent(typeof(BotAgent))]
public class BotDispleasureController : NetworkBehaviour
{
    [Header("Настройки недовольства")]
    [SerializeField] private float maxDispleasure = 100f;
    [SerializeField] private float baseDispleasureRate = 5f; // Накопление в секунду
    [SerializeField] private float decayRate = 2f;           // Спад в секунду, когда не смотрят

    [SerializeField] private NetworkVariable<float> currentDispleasure = new NetworkVariable<float>(0f);

    // Список ID игроков, которые прямо сейчас смотрят на бота
    private HashSet<ulong> watchers = new HashSet<ulong>();
    private BotAgent botAgent;

    public float CurrentDispleasure => currentDispleasure.Value;

    private void Awake()
    {
        botAgent = GetComponent<BotAgent>();
    }

    public override void OnNetworkDespawn()
    {
        watchers.Clear();
        base.OnNetworkDespawn();
    }

    private void Update()
    {
        // Вся логика накопления происходит только на сервере (плавно, каждый кадр)
        if (!IsServer) return;

        IGameTable table = GetCurrentTable();
        if (table == null || !table.IsGameStarted) return;

        if (watchers.Count > 0)
        {
            // Если кто-то смотрит, раздражение растет (можно умножать на кол-во смотрящих)
            float increase = baseDispleasureRate * Time.deltaTime * watchers.Count;
            currentDispleasure.Value = Mathf.Clamp(currentDispleasure.Value + increase, 0f, maxDispleasure);

            if (currentDispleasure.Value >= maxDispleasure)
            {
                HandleMaxDispleasure(table);
            }
        }
        else if (currentDispleasure.Value > 0)
        {
            // Если никто не смотрит, раздражение потихоньку спадает
            currentDispleasure.Value = Mathf.Clamp(currentDispleasure.Value - (decayRate * Time.deltaTime), 0f, maxDispleasure);
        }
    }

    [Rpc(SendTo.Server)]
    public void AddWatcherServerRpc(ulong clientId)
    {
        watchers.Add(clientId);
    }

    [Rpc(SendTo.Server)]
    public void RemoveWatcherServerRpc(ulong clientId)
    {
        watchers.Remove(clientId);
    }

    /// <summary>
    /// Добавляет мгновенное количество раздражения (например, при ложном шлепке).
    /// Вызывается только на сервере.
    /// </summary>
    public void AddInstantDispleasure(float amount)
    {
        if (!IsServer) return;

        currentDispleasure.Value = Mathf.Clamp(currentDispleasure.Value + amount, 0f, maxDispleasure);
        Debug.Log($"[Displeasure] Бот {gameObject.name} получил мгновенное раздражение: +{amount}. Текущее: {currentDispleasure.Value}");

        if (currentDispleasure.Value >= maxDispleasure)
        {
            HandleMaxDispleasure(GetCurrentTable());
        }
    }

    private void HandleMaxDispleasure(IGameTable table)
    {
        Debug.LogWarning($"[Displeasure] Бот {gameObject.name} вышел из себя!");
        if(table != null)
        {
            table.ForceStopGame(ulong.MaxValue, isCheaterBot: false, reason: "Harassment");
        }
        botAgent.GoToExit();
        ResetDispleasure();
    }

    public void ResetDispleasure()
    {
        if (!IsServer) return;
        currentDispleasure.Value = 0f;
        watchers.Clear();
    }
    
    private IGameTable GetCurrentTable()
    {
        if (botAgent != null && TryGetComponent<IBotGameBehavior>(out var behavior))
        {
            // ВАЖНО: В будущем лучше закэшировать текущий стол внутри BotAgent, 
            // чтобы не искать его через FindObjectsByType в Update!
            var allTables = FindObjectsByType<GameTable>(FindObjectsSortMode.None);
            foreach (var table in allTables)
            {
                if (table.IsBotOccupied && table.botNetworkObjectRef.Value.TryGet(out NetworkObject botObj))
                {
                    if (botObj.gameObject == gameObject) return table;
                }
            }
        }
        return null;
    }
}