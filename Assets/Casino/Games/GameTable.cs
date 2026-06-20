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
public abstract class GameTable : NetworkBehaviour, IInteractable, IGameTable
{
    [Header("Interaction Settings")]
    [SerializeField] private string promptText = "Занять стол (E)";
    [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
    [SerializeField] private int priority = 0;

    [SerializeField] private Transform botWaitPoint;

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

    private readonly NetworkVariable<bool> isBotOccupied = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    /// <summary>Ссылка на сетевой объект бота, закреплённого за столом (только на сервере).</summary>
    private NetworkObject currentBot;

    // ---------- Реализация IInteractable ----------
    /// <inheritdoc />
    public InteractionTriggerMode TriggerMode => triggerMode;
    /// <inheritdoc />
    public int Priority => priority;
    /// <inheritdoc />
    public string InteractionPromptText => promptText;

    // ---------- Реализация IGameTable ----------
    /// <inheritdoc />
    public bool IsOccupied => isOccupied.Value;
    /// <inheritdoc />
    public ulong OccupiedByClientId => occupiedByClientId.Value;
    /// <inheritdoc />
    public bool IsBotOccupied => isBotOccupied.Value;
    /// <inheritdoc />
    public bool IsGameStarted => gameInProgress.Value;

    /// <inheritdoc />
    public event Action<ulong> OnOccupantChanged;
    /// <inheritdoc />
    public event Action<bool> OnBotOccupancyChanged;
    /// <inheritdoc />
    public event Action OnGameStarted;
    /// <inheritdoc />
    public event Action OnGameEnded;
    /// <inheritdoc />
    public event Action<bool> OnTriggerZonePlayerChanged;

    public readonly NetworkVariable<NetworkObjectReference> botNetworkObjectRef = new NetworkVariable<NetworkObjectReference>(
    default,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    // ---------- Unity / NetworkBehaviour ----------
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

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

    private void OnGameProgressChanged(bool previous, bool current)
    {
        if (current)
            OnGameStarted?.Invoke();
        else
            OnGameEnded?.Invoke();
    }

   

    // ---------- IInteractable методы ----------
    /// <summary>Определяет, может ли игрок взаимодействовать со столом (только если стол не занят другим игроком).</summary>
    public bool CanInteract(GameObject interactor) => !isOccupied.Value;

    /// <summary>Вызывается при взаимодействии игрока. Запускает процесс занятия стола.</summary>
    public void Interact(GameObject interactor)
    {
        if (!IsSpawned || isOccupied.Value)
        {
            Debug.Log("!IsSpawned || isOccupied.Value");
            return;
        }

        NetworkObject netObj = interactor.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            OccupyServerRpc(netObj.OwnerClientId);
        }
        else Debug.Log("netObj == null");
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

        if (isOccupied.Value)
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
    /// <inheritdoc />
    public virtual void AssignBot(NetworkObject bot)
    {
        if (!IsServer) return;

        if (isBotOccupied.Value)
        {
            Debug.LogWarning($"[GameTable] Попытка назначить бота, но место уже занято.");
            return;
        }

        currentBot = bot;
        botNetworkObjectRef.Value = new NetworkObjectReference(bot);
        isBotOccupied.Value = true;
        Debug.Log($"[GameTable] Бот {bot.name} занял место за столом.");
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

        // Если шла игра – принудительно завершаем
        if (gameInProgress.Value)
        {
            Debug.LogWarning($"[GameTable] Попытка убрать бота, но идет игра.");
            return;
        }

        currentBot = null;
        botNetworkObjectRef.Value = default;
        isBotOccupied.Value = false;
        Debug.Log($"[GameTable] Бот убран из-за стола.");
    }

    // ---------- Управление игровой сессией ----------
    /// <inheritdoc />
    public virtual void StartGame()
    {
        if (!IsServer) return;

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
    public virtual void ForceStopGame(ulong winnerClientId, bool isCheaterBot, string reason)
    {
        if (!IsServer || !gameInProgress.Value) return;

        Debug.LogWarning($"[GameTable] Игра принудительно остановлена. Причина: {reason}. Победитель: {winnerClientId}. Читер бот? {isCheaterBot}");

        // Переводим состояние игры в "не активна"
        gameInProgress.Value = false;
    }

    /// <summary>
    /// Проверяет, выполнены ли все условия для начала игры.
    /// По умолчанию требуется наличие и игрока, и бота.
    /// </summary>
    /// <returns>true, если игра может быть начата.</returns>
    protected virtual bool CanStartGame()
    {
        Debug.Log($"isOccupied {isOccupied.Value}  isBotOccupied {isBotOccupied.Value}");

        return IsOccupied && IsBotOccupied;
    }

    // ---------- Обработка триггера ----------
    public virtual void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsPlayerObject)
        {
            if (occupiedByClientId.Value == netObj.OwnerClientId)
            {
                Leave(netObj.OwnerClientId);
            }   
        }
    }

    public virtual void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsPlayerObject)
        {

            OccupyServerRpc(netObj.OwnerClientId);
        }
        else Debug.Log("netObj == null");

    }

    // ---------- Обработка дисконнекта ----------
    public virtual void OnClientDisconnect(ulong clientId)
    {
        if (!IsServer) return;

        if (occupiedByClientId.Value == clientId)
        {
            
        }
    }
}