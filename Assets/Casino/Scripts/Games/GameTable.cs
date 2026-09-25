using System;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Games
{
    public enum GameType
    {
        BlackGreg,
        Roulette,
        Poker,
        Slots
    }

    /// <summary>
    /// Абстрактный базовый класс для всех игровых столов.
    /// Управляет занятостью игроком и ботом, взаимодействием с игроком, а также жизненным циклом игры.
    /// Конкретные игры наследуют этот класс и добавляют свою механику.
    /// </summary>
    public abstract class GameTable : NetworkBehaviour, IGameTable
    {
        [SerializeField] private Transform botWaitPoint;

        public Transform BotWaitPoint => botWaitPoint != null ? botWaitPoint : transform;

        // ---------- Сетевые переменные ----------
        protected readonly NetworkVariable<bool> isOccupied = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        protected readonly NetworkVariable<ulong> occupiedByClientId = new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        /// <summary>Синхронизируемый флаг, указывающий, идёт ли игра.</summary>
        protected readonly NetworkVariable<bool> gameInProgress = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        protected readonly NetworkVariable<bool> isBotOccupied = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        protected readonly NetworkVariable<bool> isBotReachedTable = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        protected readonly NetworkVariable<NetworkObjectReference> botNetworkObjectRef = new NetworkVariable<NetworkObjectReference>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        /// <summary>Ссылка на сетевой объект бота, закреплённого за столом (только на сервере).</summary>
        protected NetworkObject currentBot;

        public Transform TableTransform => transform;
        public string TableName => gameObject.name;

        // ---------- Реализация IGameTable ----------
        /// <inheritdoc />
        public bool IsOccupied => isOccupied.Value;

        /// <inheritdoc />
        public ulong OccupiedByClientId => occupiedByClientId.Value;
        /// <inheritdoc />
        public bool IsBotOccupied => isBotOccupied.Value;
        /// <inheritdoc />
        public bool IsGameStarted => gameInProgress.Value;

        public bool IsBotReachedTable => isBotReachedTable.Value;
        public abstract string TableType { get; }

        /// <inheritdoc />
        public event Action<ulong> OnOccupantChanged;
        /// <inheritdoc />
        public event Action<bool> OnBotOccupancyChanged;
        /// <inheritdoc />
        public event Action<bool> OnGameStateChanged;
        /// <inheritdoc />
        public event Action<bool> OnBotReachedTableStateChanged;


        // ---------- Unity / NetworkBehaviour ----------
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            GameTableManager.Instance?.RegisterTable(this);

            isOccupied.OnValueChanged += OnIsOccupiedChanged;
            gameInProgress.OnValueChanged += OnGameProgressChanged;
            isBotOccupied.OnValueChanged += OnBotOccupiedChanged;
            isBotReachedTable.OnValueChanged += OnBotReachedTableChanged;
        }

        public override void OnNetworkDespawn()
        {
            GameTableManager.Instance?.UnregisterTable(this);

            isOccupied.OnValueChanged -= OnIsOccupiedChanged;
            gameInProgress.OnValueChanged -= OnGameProgressChanged;
            isBotOccupied.OnValueChanged -= OnBotOccupiedChanged;
            isBotReachedTable.OnValueChanged -= OnBotReachedTableChanged;

            base.OnNetworkDespawn();
        }

        // ---------- Обработчики изменений NetworkVariable ----------
        private void OnIsOccupiedChanged(bool previous, bool current)
        {
            ulong clientId = current ? occupiedByClientId.Value : ulong.MaxValue;
            OnOccupantChanged?.Invoke(clientId);
            Debug.Log($"IsOccupied == {IsOccupied}");
        }

        private void OnBotOccupiedChanged(bool previous, bool current)
        {
            OnBotOccupancyChanged?.Invoke(current);
        }

        public void OnGameProgressChanged(bool previous, bool current)
        {
            OnGameStateChanged?.Invoke(current);
        }

        public void OnBotReachedTableChanged(bool previous, bool current)
        {
            OnBotReachedTableStateChanged?.Invoke(current); 
        }


        // ---------- RPC для занятия/освобождения игрока ----------
        [Rpc(SendTo.Server)]
        public void OccupyServerRpc(ulong clientId)
        {
            if (!IsServer || isOccupied.Value) return;
            Occupy(clientId);
        }

        [Rpc(SendTo.Server)]
        public void LeaveServerRpc(ulong clientId)
        {
            if (!IsServer) return;
            Leave(clientId);
        }

        // ---------- Управление игроком (сервер) ----------
        /// <inheritdoc />
        public virtual void Occupy(ulong clientId)
        {
            if (!IsServer) return;
            if (IsOccupied)
            {
                Debug.LogWarning($"[GameTable] Попытка занять уже занятый стол клиентом {clientId}");
                return;
            }

            occupiedByClientId.Value = clientId; // ← СНАЧАЛА владелец
            isOccupied.Value = true;             // ← ПОТОМ флаг занятости
            Debug.Log($"[GameTable] Стол занят клиентом {clientId}");
        }

        /// <inheritdoc />
        public virtual void Leave(ulong clientId)
        {
            if (!IsServer) return;

            if (occupiedByClientId.Value != clientId)
            {
                Debug.LogWarning($"[GameTable] Клиент {clientId} пытался покинуть стол, но владелец {occupiedByClientId.Value} стол занят");
                return;
            }

            isOccupied.Value = false;
            occupiedByClientId.Value = ulong.MaxValue;
            Debug.Log($"[GameTable] Стол освобождён клиентом {clientId}");
        }

        // ---------- Управление ботом (сервер) ----------

        /// <summary>
        /// Проверяет, может ли бот быть назначен на этот стол.
        /// Базовая проверка: стол не занят другим ботом.
        /// Наследники могут переопределять для дополнительных проверок (например, сломан/взорван).
        /// Вызывается на сервере.
        /// </summary>
        public virtual bool CanAssignBot()
        {
            // Базовое условие: бот уже не занимает это место
            return !IsBotOccupied;
        }

        /// <inheritdoc />
        public virtual void AssignBot(NetworkObject bot)
        {
            if (!IsServer) return;

            if (!CanAssignBot())
            {
                Debug.LogWarning($"[GameTable] Попытка назначить бота, но место уже занято.");
                return;
            }

            currentBot = bot;
            botNetworkObjectRef.Value = new NetworkObjectReference(bot);
            isBotOccupied.Value = true;
            Debug.Log($"[GameTable] Бот {bot.GetEntityId()} занял место за столом.");
        }

        public virtual void BotReachedTable(bool reached)
        {
            isBotReachedTable.Value = reached;
        }

        /// <inheritdoc />
        public virtual void RemoveBot()
        {
            if (!IsServer) return;

            if (!isBotOccupied.Value)
            {
                Debug.LogWarning($"[GameTable] Попытка убрать бота, но место не занято.");
                return;
            }

            gameInProgress.Value = false;
            currentBot = null;
            botNetworkObjectRef.Value = default;
            isBotOccupied.Value = false;
            isBotReachedTable.Value = false;
            GameTableManager.Instance?.NotifyTableFreed();
            Debug.Log($"[GameTable] Бот убран из-за стола.");
        }

        // ---------- Управление игровой сессией ----------
        /// <inheritdoc />
        public virtual void StartGame()
        {
            if (!IsServer)
            {
                Debug.LogWarning($"[SlotMachine] Попытка начать игру на клиенте. Игровая логика должна выполняться только на сервере.");
                return;
            }

            if (!CanStartGame())
            {
                Debug.LogWarning($"[GameTable] Невозможно начать игру: нет игрока или бота.");
                return;
            }

            gameInProgress.Value = true;
        }

        /// <inheritdoc />
        public virtual void EndGame()
        {
            if (!IsServer || !gameInProgress.Value) return;

            gameInProgress.Value = false;
        }

        /// <inheritdoc />
        public virtual void ForceStopGame(bool isCheaterBot, string reason)
        {
            if (!IsServer || !gameInProgress.Value) return;

            Debug.LogWarning($"[GameTable] Игра принудительно остановлена. Причина: {reason}.");

            EndGame();
        }

        /// <summary>
        /// Проверяет, выполнены ли все условия для начала игры.
        /// </summary>
        /// <returns>true, если игра может быть начата.</returns>
        protected virtual bool CanStartGame()
        {
            return true;
        }

        public virtual void OnCheaterCaught(ulong accuserClientId, bool isCheaterBot)
        {
            // Поведение по умолчанию: форс-стоп игры. 
            // Конкретные столы (например, BlackGregTable) могут переопределить.
            ulong winnerId = isCheaterBot ? accuserClientId : ulong.MaxValue;
            ForceStopGame(isCheaterBot, "Cheating");
        }
    }
}