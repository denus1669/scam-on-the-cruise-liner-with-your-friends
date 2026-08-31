using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Агрегированная статистика по одному игроку-сотруднику казино.
/// </summary>
[Serializable]
public struct PlayerStatistics : INetworkSerializable
{
    public ulong PlayerId;
    public int TotalDeposits;      // Сколько игрок принёс в кассу
    public int TotalWithdraws;     // Сколько игрок забрал из кассы
    public int TransactionCount;   // Общее количество операций

    /// <summary>Чистый вклад в кассу (может быть отрицательным).</summary>
    public int NetContribution => TotalDeposits - TotalWithdraws;

    // Реализация сетевой сериализации для NGO
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerId);
        serializer.SerializeValue(ref TotalDeposits);
        serializer.SerializeValue(ref TotalWithdraws);
        serializer.SerializeValue(ref TransactionCount);
    }
}

/// <summary>
/// Серверный сборщик статистики.
/// Слушает события кассы, ведёт сырой лог и агрегирует данные по игрокам.
/// НЕ занимается UI, графиками или VFX — только хранит и отдаёт данные.
/// </summary>
public class StatisticsCollector : NetworkBehaviour
    {
        [Header("Зависимости")]
        [SerializeField] private CasinoBank casinoBank;

        // Сырой лог всех транзакций (для графиков и детальных отчётов)
        private readonly List<BankTransaction> _transactionLog = new List<BankTransaction>();

        // Агрегированная статистика по каждому игроку
        private readonly Dictionary<ulong, PlayerStatistics> _playerStats = new Dictionary<ulong, PlayerStatistics>();

        /// <summary>
        /// Срабатывает на сервере при регистрации новой транзакции.
        /// Будущие системы (достижения, отчеты) могут подписываться на это событие.
        /// </summary>
        public event Action<BankTransaction> OnTransactionRegistered;
        /// <summary>
        /// Событие на клиенте. Срабатывает при получении обновлённой статистики от сервера.
        /// </summary>
        public event Action<PlayerStatistics[]> OnClientStatsUpdated;


    [ClientRpc]
    private void PushStatsToClientsClientRpc(PlayerStatistics[] statsArray)
    {
        OnClientStatsUpdated?.Invoke(statsArray);
    }

    /// <summary>
    /// Клиент запрашивает актуальный снапшот статистики (например, при входе в лобби).
    /// </summary>
    [ServerRpc]
    public void RequestCurrentStatsServerRpc(ServerRpcParams rpcParams = default)
    {
        var statsArray = _playerStats.Values.ToArray();

        // Отправляем данные только тому клиенту, который их запросил
        ClientRpcParams clientParams = new ClientRpcParams
        {
            Send = { TargetClientIds = new[] { rpcParams.Receive.SenderClientId } }
        };

        PushStatsToClientClientRpc(statsArray, clientParams);
    }

    [ClientRpc]
    private void PushStatsToClientClientRpc(PlayerStatistics[] statsArray, ClientRpcParams clientParams = default)
    {
        OnClientStatsUpdated?.Invoke(statsArray);
    }
    private void Reset()
        {
            casinoBank = GetComponentInParent<CasinoBank>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsServer) return;

            if (casinoBank == null)
            {
                Debug.LogError("[StatisticsCollector] CasinoBank не назначен!");
                return;
            }

            casinoBank.OnTransactionCompleted += HandleTransactionCompleted;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && casinoBank != null)
            {
                casinoBank.OnTransactionCompleted -= HandleTransactionCompleted;
            }

            base.OnNetworkDespawn();
        }

        private void HandleTransactionCompleted(BankTransaction transaction)
        {
            // 1. Сохраняем в сырой лог (для графиков)
            _transactionLog.Add(transaction);

            // 2. Обновляем агрегированную статистику по оператору
            UpdatePlayerStats(transaction);

            // 3. Оповещаем подписчиков на сервере
            OnTransactionRegistered?.Invoke(transaction);

            Debug.Log($"[Statistics] Зарегистрирована транзакция: {transaction.Type} {transaction.Amount} от {transaction.OperatorClientId} ({transaction.Reason})");

            // 4. ПУШИМ ОБНОВЛЕНИЯ ВСЕМ КЛИЕНТАМ
            if (IsServer)
            {
                PushStatsToClientsClientRpc(_playerStats.Values.ToArray());
            }
    }

        private void UpdatePlayerStats(BankTransaction transaction)
        {
            // Системные операции (ulong.MaxValue) не привязываем к игрокам
            if (transaction.OperatorClientId == ulong.MaxValue) return;

            if (!_playerStats.TryGetValue(transaction.OperatorClientId, out PlayerStatistics stats))
            {
                stats = new PlayerStatistics { PlayerId = transaction.OperatorClientId };
            }

            switch (transaction.Type)
            {
                case TransactionType.Deposit:
                    stats.TotalDeposits += transaction.Amount;
                    break;
                case TransactionType.Withdraw:
                    stats.TotalWithdraws += transaction.Amount;
                    break;
            }

            stats.TransactionCount++;
            _playerStats[transaction.OperatorClientId] = stats;
        }

        // ========== ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ ОТЧЁТОВ ==========

        /// <summary>
        /// Возвращает копию сырого лога транзакций (для построения графиков).
        /// </summary>
        public IReadOnlyList<BankTransaction> GetTransactionLog() => _transactionLog;

        /// <summary>
        /// Возвращает агрегированную статистику по всем игрокам.
        /// </summary>
        public IReadOnlyDictionary<ulong, PlayerStatistics> GetAllPlayerStats() => _playerStats;

        /// <summary>
        /// Возвращает статистику конкретного игрока.
        /// </summary>
        public bool TryGetPlayerStats(ulong playerId, out PlayerStatistics stats)
        {
            return _playerStats.TryGetValue(playerId, out stats);
        }

        /// <summary>
        /// Будущий метод для регистрации шлепков (заглушка для расширяемости).
        /// </summary>
        public void RegisterSlap(ulong slapperId, string botState, string tableType)
        {
            // TODO: Добавить структуру SlapEvent и список _slapLog
            Debug.Log($"[Statistics] (заглушка) Зарегистрирован шлепок: игрок {slapperId}, состояние бота {botState}, стол {tableType}");
        }

        /// <summary> 
        /// Будущий метод для регистрации исхода раунда (заглушка для расширяемости).
        /// </summary>
        public void RegisterRoundOutcome(ulong playerId, string tableType, string outcome, bool wasCheatInvolved)
        {
            // TODO: Добавить структуру RoundOutcome и список _roundLog
            Debug.Log($"[Statistics] (заглушка) Исход раунда: игрок {playerId}, стол {tableType}, результат {outcome}, мухлеж {wasCheatInvolved}");
        }
    }
