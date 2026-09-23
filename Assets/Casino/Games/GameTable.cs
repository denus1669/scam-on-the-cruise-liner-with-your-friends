using Assets.Casino.Bank;
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

        [Header("Экономика")]
        [SerializeField] protected CasinoBank casinoBank;
        [SerializeField] private int anteAmount = 1;

        [Header("Ссылки")]
        [SerializeField] private BoxCollider boxCollider;
        private Vector3 readyGameCollider = new Vector3(0, 0, -1);
        private Vector3 waitingGameCollider = new Vector3(0, -100, 1);


        public Transform BotWaitPoint => botWaitPoint != null ? botWaitPoint : transform;

        // ---------- Сетевые переменные ----------
        private readonly NetworkVariable<bool> isOccupied = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<ulong> occupiedByClientId = new NetworkVariable<ulong>(
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

        public readonly NetworkVariable<NetworkObjectReference> botNetworkObjectRef = new NetworkVariable<NetworkObjectReference>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public NetworkList<ulong> playersInGameArea = new NetworkList<ulong>();


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
            boxCollider.center = waitingGameCollider;
            isBotReachedTable.OnValueChanged += OnBotReachedTableChanged;
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

        }

        public override void OnNetworkDespawn()
        {
            GameTableManager.Instance?.UnregisterTable(this);

            isOccupied.OnValueChanged -= OnIsOccupiedChanged;
            gameInProgress.OnValueChanged -= OnGameProgressChanged;
            isBotOccupied.OnValueChanged -= OnBotOccupiedChanged;
            boxCollider.center = waitingGameCollider;
            isBotReachedTable.OnValueChanged -= OnBotReachedTableChanged;


            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;

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

        public void BotReachedTable(bool reached)
        {
            boxCollider.center = readyGameCollider;
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

            boxCollider.center = waitingGameCollider;

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


            if (IsServer && casinoBank != null)
            {
                casinoBank.TryWithdraw(anteAmount, occupiedByClientId.Value, "Ставка", TableType);
            }
        }

        /// <inheritdoc />
        public virtual void EndGame()
        {
            if (!IsServer || !gameInProgress.Value) return;

            gameInProgress.Value = false;
        }

        /// <inheritdoc />
        public virtual void ForceStopGame(ulong winnerClientId, bool isCheaterBot, string reason)
        {
            if (!IsServer || !gameInProgress.Value) return;

            Debug.LogWarning($"[GameTable] Игра принудительно остановлена. Причина: {reason}.");

            // ВОЗВРАТ СТАВКИ: Если игра прервана извне (конец дня), возвращаем анте игроку
            if (casinoBank != null && IsOccupied && occupiedByClientId.Value != ulong.MaxValue && reason != "Cheating")
            {
                bool refunded = casinoBank.TryDeposit(anteAmount, occupiedByClientId.Value, "Возврат ставки день завершен", TableType);
                if (refunded)
                {
                    Debug.Log($"[GameTable] Ставка ({anteAmount}) возвращена игроку {occupiedByClientId.Value} из-за конца дня.");
                    // Можно добавить ClientRpc, чтобы показать игроку всплывающий текст "+1 фишка (возврат)"
                }
            }
            // Переводим состояние игры в "не активна"
            EndGame();
        }

        /// <summary>
        /// Проверяет, выполнены ли все условия для начала игры.
        /// По умолчанию требуется наличие и игрока, и бота.
        /// </summary>
        /// <returns>true, если игра может быть начата.</returns>
        protected virtual bool CanStartGame()
        {
            Debug.Log($"isOccupied {isOccupied.Value}  isBotOccupied {isBotOccupied.Value}");

            // Базовая проверка: есть ли игрок и бот
            if (!IsOccupied || !IsBotOccupied)
            {
                Debug.LogWarning($"[GameTable] Невозможно начать игру: нет игрока или бота.");
                return false;
            }

            // Проверка наличия средств в кассе для обеспечения игры
            if (casinoBank == null)
            {
                Debug.LogWarning($"[GameTable] CasinoBank не назначен, игра не может начаться.");
                return false;
            }

            // Требуется минимум anteAmount фишек, чтобы обеспечить потенциальный выигрыш игрока
            if (casinoBank.CurrentBalance < anteAmount)
            {
                Debug.LogWarning($"[GameTable] Недостаточно средств в кассе для начала игры. " +
                                 $"Требуется: {anteAmount}, доступно: {casinoBank.CurrentBalance}");
                return false;
            }

            return true;
        }

        public virtual void OnCheaterCaught(ulong accuserClientId, bool isCheaterBot)
        {
            // Поведение по умолчанию: форс-стоп игры. 
            // Конкретные столы (например, BlackGregTable) могут переопределить.
            ulong winnerId = isCheaterBot ? accuserClientId : ulong.MaxValue;
            ForceStopGame(winnerId, isCheaterBot, "Cheating");
        }

        // ---------- Обработка триггера ----------
        public virtual void OnTriggerExit(Collider other)
        {
            if (!IsServer)
            {
                Debug.LogWarning($"[GameTable] Попытка покинуть стол но это не сервер");
                return;
            }

            // 1. Получаем NetworkObject вышедшего коллайдера
            NetworkObject netObj = other.GetComponent<NetworkObject>();

            // Если это не сетевой объект или не игрок — игнорируем
            if (netObj == null || !netObj.IsPlayerObject)
            {
                Debug.LogWarning($"[GameTable] Попытка покинуть стол но netObj == null {netObj == null} а !netObj.IsPlayerObject {!netObj.IsPlayerObject}");
                return;
            }
            Debug.Log($"OnTriggerExitnetObj.OwnerClientId   {netObj.OwnerClientId}");


            ulong exitingClientId = netObj.OwnerClientId;
            RemovePlayer(exitingClientId);

            // 2. Проверяем, является ли вышедший игрок ТЕКУЩИМ владельцем стола
            if (IsOccupied)
            {
                Debug.LogWarning($"[GameTable] Попытка покинуть стол занятый стол");

                if (exitingClientId != occupiedByClientId.Value)
                {
                    Debug.LogWarning($"[GameTable] Попытка покинуть стол занятый стол exitingClientId {exitingClientId} != occupiedByClientId.Value {occupiedByClientId.Value}");

                    // Если вышел кто-то другой (второй игрок, зритель и т.д.), просто игнорируем
                    return;
                }
            }

            // 3. Если вышел именно владелец, освобождаем стол
            LeaveServerRpc(exitingClientId);

        }

        public virtual void OnTriggerEnter(Collider other)
        {
            if (!IsServer)
            {
                Debug.LogWarning($"[GameTable] Попытка зайти в стол но это не сервер");
                return;
            }


            NetworkObject netObj = other.GetComponent<NetworkObject>();

            if (netObj == null || !netObj.IsPlayerObject)
            {
                Debug.LogWarning($"[GameTable] Попытка зайти в стол но netObj == null {netObj == null} а !netObj.IsPlayerObject {!netObj.IsPlayerObject}");
                return;
            }

            ulong exitingClientId = netObj.OwnerClientId;


            Debug.Log($"OnTriggerEnternetObj.OwnerClientId   {exitingClientId}");

            if (IsOccupied)
            {
                if (exitingClientId != occupiedByClientId.Value)
                {
                    Debug.LogWarning($"[GameTable] Попытка зайти в стол но он уже занят");
                    return;
                }
            }

            AddPlayer(exitingClientId);

            if (IsGameStarted)
            {
                OccupyServerRpc(exitingClientId);
            }
        }
        protected virtual void AddPlayer(ulong exitingClientId)
        {

            if (playersInGameArea.Contains(exitingClientId))
            {
                Debug.LogWarning($"[GameTable] нельзя добавить уже имеющегося игрока в списке playersInGameArea {exitingClientId}");
                return;
            }

            playersInGameArea.Add(exitingClientId);
            Debug.Log($"[GameTable] AddPlayer: {exitingClientId}, count={playersInGameArea.Count}");
        }

        protected virtual void RemovePlayer(ulong exitingClientId)
        {
            if (playersInGameArea.Count < 0)
            {
                Debug.LogWarning($"[GameTable] Нельзя убрать никого нет BeforeRemove {exitingClientId}, count={playersInGameArea.Count} ");
                return;
            }
            playersInGameArea.Remove(exitingClientId);
            Debug.Log($"[GameTable] Remove {exitingClientId}, count={playersInGameArea.Count} ");
        }

        // ---------- Обработка дисконнекта ----------
        public virtual void OnClientDisconnect(ulong clientId)
        {
            if (!IsServer) return;

            if (occupiedByClientId.Value == clientId)
            {
                RemovePlayer(clientId);
                Leave(clientId);
            }
        }

    }
}