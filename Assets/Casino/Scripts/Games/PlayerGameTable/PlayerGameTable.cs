using Assets.Casino.Bank;
using Assets.Casino.Games;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerGameTableHighlighter))]
public abstract class PlayerGameTable : GameTable
{
    [Header("Экономика")]
    [SerializeField] protected CasinoBank casinoBank;
    [SerializeField] private int anteAmount = 1;

    [Header("Ссылки")]
    [SerializeField] private BoxCollider boxCollider;
    private Vector3 readyGameCollider = new Vector3(0, 0, -1);
    private Vector3 waitingGameCollider = new Vector3(0, -100, 1);
    public NetworkList<ulong> playersInGameArea = new NetworkList<ulong>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (boxCollider != null)
            boxCollider.center = waitingGameCollider;
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    public override void OnNetworkDespawn()
    {
        if (boxCollider != null)
            boxCollider.center = waitingGameCollider;
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// Проверяет, выполнены ли все условия для начала игры.
    /// По умолчанию требуется наличие и игрока, и бота.
    /// </summary>
    /// <returns>true, если игра может быть начата.</returns>
    /// 
    protected override bool CanStartGame()
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

    public override void StartGame()
    {
        base.StartGame();
        if (IsServer && casinoBank != null)
        {
            casinoBank.TryWithdraw(anteAmount, occupiedByClientId.Value, "Ставка", TableType);
        }
    }

    public override void BotReachedTable(bool reached)
    {
        if (boxCollider != null)
            boxCollider.center = readyGameCollider;
        base.BotReachedTable(reached);
    }

    public override void RemoveBot()
    {
        base.RemoveBot();

        if (boxCollider != null)
            boxCollider.center = waitingGameCollider;
    }

    public override void ForceStopGame(bool isCheaterBot, string reason)
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

    // ---------- Обработка триггера ----------

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
