using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerCheatController : CheatController
{
    [Header("Minigame Integration")]
    [Tooltip("Ссылка на менеджер мини-игр на этом же игроке")]
    [SerializeField] private PlayerMinigameManager _minigameManager;

    private void Awake()
    {
        if (_minigameManager == null)
        {
            _minigameManager = GetComponent<PlayerMinigameManager>();
            if (_minigameManager == null)
                Debug.LogError("[PlayerCheatController] PlayerMinigameManager не найден на объекте игрока!");
        }
    }

    /// <summary>
    /// Запрос от клиента на сервер о начале мухлежа.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void RequestCheatServerRpc(string specificCheatName = "")
    {
        if (!IsServer) return;
        if (IsCheating) return;

        Debug.Log($"[CheatController] Игрок {OwnerClientId} запросил мухлеж: {specificCheatName}");

        IGameTable currentTable = GameTableManager.Instance?.GetTableOccupiedByPlayer(OwnerClientId);

        if (currentTable == null)
        {
            Debug.LogWarning($"[CheatController] Игрок {OwnerClientId} не сидит за столом.");
            return;
        }

        CheatAction cheatToExecute = null;
        if (!string.IsNullOrEmpty(specificCheatName))
            // ИСПРАВЛЕНИЕ 1: Ищем по CheatName, а не по name ассета
            cheatToExecute = availableCheats.Find(c => c.name == specificCheatName);
        else
            Debug.LogError("specificCheatName is null or empty!");

        if (cheatToExecute != null)
        {
            TryInitiateCheat(currentTable, cheatToExecute);
        }
        else
            Debug.LogError($"cheatToExecute == null for name: {specificCheatName}");
    }

    public override bool TryInitiateCheat(IGameTable table, CheatAction specificCheat)
    {
        if (!IsServer || IsCheating) return false;

        if (!specificCheat.CanExecute(table, "Player"))
        {
            Debug.LogWarning($"[CheatController] Чит {specificCheat.name} нельзя выполнить за этим столом.");
            return false;
        }

        currentTable = table;
        CheatRoutine(specificCheat);
        return true;
    }

    protected override void CheatRoutine(CheatAction cheat)
    {
        base.CheatRoutine(cheat);
        PlayerCheatRoutine(cheat);
    }

    private void PlayerCheatRoutine(CheatAction cheat)
    {
        Debug.Log($"[CheatController] Сервер запустил рутину мухлежа: {cheat.CheatName}");

        // Уведомляем клиентов об анимации
        NotifyCheatStartedClientRpc(cheat.AnimationTriggerName);

        // Запускаем мини-игру у владельца
        // Передаем имя чита как контекст, чтобы мини-игра знала, что именно отображать/проверять
        StartPlayerMinigameClientRpc(cheat.name);
    }

    [ClientRpc]
    private void StartPlayerMinigameClientRpc(string cheatActionName)
    {
        if (!IsOwner) return;

        if (_minigameManager == null)
        {
            Debug.LogError("[PlayerCheatController] MinigameManager отсутствует, невозможно запустить UI.");
            return;
        }

        // Получаем все зарегистрированные игры
        var availableGames = _minigameManager.GetAllGameKeys();

        if (availableGames.Count == 0)
        {
            Debug.LogError("[PlayerCheatController] Нет доступных мини-игр!");
            return;
        }

        // Выбираем случайную
        int randomIndex = UnityEngine.Random.Range(0, availableGames.Count);
        string minigameTypeKey = availableGames[randomIndex];

        Debug.Log($"[PlayerCheatController] Выбрана случайная мини-игра: '{minigameTypeKey}'");
        Debug.Log($"[PlayerCheatController] Запрос мини-игры '{minigameTypeKey}' через менеджер. Контекст: {cheatActionName}");

        _minigameManager.RequestStartGame(minigameTypeKey, cheatActionName, HandleMinigameResult);
    }

    private void HandleMinigameResult(bool isSuccess)
    {
        Debug.Log($"[PlayerCheatController] Мини-игра завершена локально. Успех: {isSuccess}. Отправка на сервер...");
        FinishPlayerCheatServerRpc(isSuccess);
    }

    /// <summary>
    /// Вызывается сервером через RPC, когда чит прерван инспектором или другим игроком.
    /// </summary>
    public void ForceCloseActiveMinigame()
    {
        if (_minigameManager == null) return;

        var activeGame = _minigameManager.GetActiveGame();
        if (activeGame != null)
        {
            Debug.Log("[PlayerCheatController] Мини-игра принудительно закрыта из-за поимки.");
            activeGame.ForceClose();
        }
    }

    [Rpc(SendTo.Server)]
    public void FinishPlayerCheatServerRpc(bool isSuccess)
    {
        if (!IsServer) return;

        if (!IsCheating)
        {
            Debug.LogWarning($"[CheatController] Игрок {OwnerClientId} прислал результат, но флаг isCheating = false (возможно таймаут).");
            return;
        }

        Debug.Log($"[CheatController] Сервер получил результат от {OwnerClientId}: Успех={isSuccess}");

        if (cheatCoroutine != null) StopCoroutine(cheatCoroutine);

        if (isSuccess)
        {
            Debug.Log("[CheatController] Игрок успешно совершил чит");
            CompleteCheat(currentCheatAction, "Player");
        }
        else
        {
            Debug.Log($"[CheatController] Игрок {OwnerClientId} провалил мини-игру.");
            HandleCheatCaught(ulong.MaxValue);
        }
    }
}   