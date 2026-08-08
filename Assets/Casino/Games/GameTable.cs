using Blocks.Gameplay.Core;
using System;
using System.ComponentModel.Design.Serialization;
using Unity.Netcode;
using UnityEngine;

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
    protected readonly NetworkVariable<bool> gameInProgress = new NetworkVariable<bool>(false);

    protected readonly NetworkVariable<bool> isBotOccupied = new NetworkVariable<bool>(
        false,
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

    public abstract string TableType { get; }

    /// <inheritdoc />
    public event Action<ulong> OnOccupantChanged;
    /// <inheritdoc />
    public event Action<bool> OnBotOccupancyChanged;
    /// <inheritdoc />
    public event Action OnGameStarted;
    /// <inheritdoc />
    public event Action OnGameEnded;

    public readonly NetworkVariable<NetworkObjectReference> botNetworkObjectRef = new NetworkVariable<NetworkObjectReference>(
    default,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    // ---------- Unity / NetworkBehaviour ----------
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        GameTableManager.Instance?.RegisterTable(this);

        isOccupied.OnValueChanged += OnIsOccupiedChanged;
        gameInProgress.OnValueChanged += OnGameProgressChanged;
        isBotOccupied.OnValueChanged += OnBotOccupiedChanged;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

        Debug.Log($"isOccupied {isOccupied.Value}  isBotOccupied {isBotOccupied.Value}");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        GameTableManager.Instance?.UnregisterTable(this);

        isOccupied.OnValueChanged -= OnIsOccupiedChanged;
        gameInProgress.OnValueChanged -= OnGameProgressChanged;
        isBotOccupied.OnValueChanged -= OnBotOccupiedChanged;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    // ---------- Обработчики изменений NetworkVariable ----------
    private void OnIsOccupiedChanged(bool previous, bool current)
    {
        ulong clientId = current ? occupiedByClientId.Value : ulong.MaxValue;
        OnOccupantChanged?.Invoke(clientId);
    }

    private void OnBotOccupiedChanged(bool previous, bool current)
    {
        OnBotOccupancyChanged?.Invoke(current);
    }

    public void OnGameProgressChanged(bool previous, bool current)
    {
        if (current)
            OnGameStarted?.Invoke();
        else
            OnGameEnded?.Invoke();
    }

   

    // ---------- RPC для занятия/освобождения игрока ----------
    [Rpc(SendTo.Server)]
    private void OccupyServerRpc(ulong clientId)
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

        isOccupied.Value = true;
        occupiedByClientId.Value = clientId;
        Debug.Log($"[GameTable] Стол занят клиентом {clientId}");
    }

    /// <inheritdoc />
    public virtual void Leave(ulong clientId)
    {
        if (!IsServer) return;

        if (occupiedByClientId.Value != clientId)
        {
            Debug.LogWarning($"[GameTable] Клиент {clientId} пытался покинуть стол, но владелец {occupiedByClientId.Value}");
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
        if (casinoBank != null && IsOccupied && occupiedByClientId.Value != ulong.MaxValue)
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

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsPlayerObject) return;

        if (IsServer)
        {
            if (occupiedByClientId.Value == netObj.OwnerClientId)
            {
                Leave(netObj.OwnerClientId);
            }
        }
        else if (netObj.IsLocalPlayer)
        {
            LeaveServerRpc(netObj.OwnerClientId);
        }
    }

    public virtual void OnTriggerEnter(Collider other)
    {

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsPlayerObject) return;

        if (IsServer)
        {
            // Если физика на сервере - занимаем сразу
            Occupy(netObj.OwnerClientId);
        }
        else if (netObj.IsLocalPlayer)
        {
            // Если физика на клиенте (DA) - шлем RPC
            OccupyServerRpc(netObj.OwnerClientId);
        }

    }

    // ---------- Обработка дисконнекта ----------
    public virtual void OnClientDisconnect(ulong clientId)
    {
        if (!IsServer) return;

        if (occupiedByClientId.Value == clientId)
        {
            Leave(clientId);
        }
    }
    
}