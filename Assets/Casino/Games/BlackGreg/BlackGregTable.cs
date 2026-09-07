using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Компонент стола для игры в блэкджек.
/// Наследует базовую логику GameTable и реализует ICardGameTable.
/// </summary>
public class BlackGregTable : GameTable, ICardGameTable
{
    [Header("Card Settings")]
    [SerializeField] private CardView cardViewPrefab;
    [SerializeField] private Transform playerHandParent;
    [SerializeField] private Transform botHandParent;
    [SerializeField] private Transform cardTablePosition; // точка для сброшенных/нераспределённых карт
    [Header("Reveal Settings")]
    [SerializeField] private Transform revealBotPosition; // Куда выкладывать карты боту (це    нтр стола)
    [SerializeField] private Transform revealPlayerPosition; // Куда выкладывать карты игроку (центр стола)


    [Header("Positioning")]
    [SerializeField] private Vector3 startPosition = new Vector3(0f, 0f, 0f);
    [SerializeField] private float spreadDistance = 0.22f;
    [SerializeField] private Vector3 startRotation = new Vector3(30, 180, 0);

    [Header("Game Rules")]
    [SerializeField] private int cardLimit = 10;
    [SerializeField] private int minCardsToFinish = 2;

    private readonly NetworkVariable<bool> _isRevealed = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Состояние бота
    private readonly NetworkVariable<bool> _botHasStood = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    public bool IsRevealed => _isRevealed.Value;

    public bool BotHasStood => _botHasStood.Value;


    // Данные рук (сервер)
    [SerializeField] private List<CardData> playerHandData = new List<CardData>();
    [SerializeField] private List<CardData> botHandData = new List<CardData>();

    // Визуал на клиентах
    private List<CardView> _botSpawnedCardViews = new List<CardView>();
    private List<CardView> _playerSpawnedCardViews = new List<CardView>();
    private bool placeNextCardOnLeft = true;

    // Копия руки бота до мухлежа. Хранится только пока активен мухлеж в текущем раунде.
    private List<CardData> _originalBotHand;
    private List<CardData> _originalPlayerHand;

    public override string TableType => "BlackGreg";

    // ---------- Реализация ICardGameTable ----------
    public int GetBotScore() => CalculateHandValue(botHandData);
    public int GetPlayerScore() => CalculateHandValue(playerHandData);
    public int GetBotCardCount() => botHandData.Count;
    public int GetPlayerCardCount() => playerHandData.Count;

    public List<CardData> GetBotHandCopy()
    {
        return new List<CardData>(botHandData);
    }

    public void BotDrawCard()
    {
        if (!IsServer || !IsGameStarted) return;

        if (AddCardToHand(botHandData))
        {
            CardData lastCard = botHandData[botHandData.Count - 1];
            Debug.Log($"[BlackGregTable] Бот взял карту: {lastCard.rank} {lastCard.suit} (тип: {lastCard.type}). Текущий счёт: {CalculateHandValue(botHandData)}");
            SyncHandsClientRpc(OccupiedByClientId, playerHandData.ToArray(), botHandData.ToArray());
        }

    }

    [Rpc(SendTo.Server)]
    public void BotStandServerRpc()
    {
        if (!IsServer) return;

        Debug.Log("BotStand");
        _botHasStood.Value = true;
        PutOnTableBotCards();
    }
    public void BotStand()
    {
        if (!IsServer) return;

        BotStandServerRpc();
    }

    /// <summary>
    /// Полностью перезаписывает руку бота (вызывается из системы мухлежа).
    /// </summary>
    public void OverwriteBotHand(List<CardData> newHand)
    {
        if (!IsServer) return;

        // Сохраняем копию перед первым мухлежом в раунде
        if (_originalBotHand == null)
        {
            _originalBotHand = new List<CardData>(botHandData);
            Debug.Log($"[BlackGregTable] Сохранена оригинальная рука бота ({_originalBotHand.Count} карт)");
        }

        botHandData = new List<CardData>(newHand);
        SyncHandsClientRpc(OccupiedByClientId, playerHandData.ToArray(), botHandData.ToArray());
    }

    private void RevertBotHand()
    {
        if (_originalBotHand == null)
        {
            Debug.LogWarning("[BlackGregTable] Попытка откатить руку, но копия отсутствует.");
            return;
        }

        botHandData = _originalBotHand;
        _originalBotHand = null;
        Debug.Log($"[BlackGregTable] Рука бота откачена до оригинальной ({botHandData.Count} карт)");
        SyncHandsClientRpc(OccupiedByClientId, playerHandData.ToArray(), botHandData.ToArray());
    }

    public void OverwritePlayerHand(List<CardData> newHand)
    {
        if (!IsServer) return;

        // Сохраняем копию перед первым мухлежом в раунде
        if (_originalPlayerHand == null)
        {
            _originalPlayerHand = new List<CardData>(playerHandData);
            Debug.Log($"[BlackGregTable] Сохранена оригинальная рука игрока ({_originalPlayerHand.Count} карт)");
        }

        playerHandData = new List<CardData>(newHand);
        SyncHandsClientRpc(OccupiedByClientId, playerHandData.ToArray(), botHandData.ToArray());
    }

    // ---------- Переопределение жизненного цикла ----------
    public override void StartGame()
    {
        if (!IsServer || !CanStartGame()) return;

        // Сброс состояния для новой игры
        playerHandData.Clear();
        botHandData.Clear();
        _botHasStood.Value = false;
        _originalBotHand = null;
        _isRevealed.Value = false;

        Debug.Log("CanStartGame " + CanStartGame());
        base.StartGame(); // устанавливает gameInProgress.Value = true
    }

    public override void EndGame()
    {
        if (!IsServer || !IsGameStarted) return;

        _isRevealed.Value = false; // Сброс флага
        _originalBotHand = null;
        base.EndGame(); // устанавливает gameInProgress.Value = false
        ClearHands();   // очистка визуала и данных
        ClearHandsClientRpc();
    }
    [ClientRpc]
    private void ClearHandsClientRpc()
    {
        CardViewCleaner(_playerSpawnedCardViews);
        CardViewCleaner(_botSpawnedCardViews);
    }

    // ---------- Методы для игрока (вызываются UI) ----------
    public void PlayerDrawCard()
    {
        if (!IsServer) return;

        if (!IsGameStarted)
        {
            if (!CanStartGame())    // нет обоих участников – выходим
                return;
            StartGame();
        }

        if (AddCardToHand(playerHandData))
        {
            CardData lastCard = playerHandData[playerHandData.Count - 1];
            Debug.Log($"[BlackGregTable] Игрок взял карту: {lastCard.rank} {lastCard.suit} (тип: {lastCard.type}). Текущий счёт: {CalculateHandValue(playerHandData)}");
            SyncHandsClientRpc(OccupiedByClientId, playerHandData.ToArray(), botHandData.ToArray());
        }
    }

    public void FinishGame()
    {
        if (!IsServer || !IsGameStarted || !_isRevealed.Value) return;

        if (playerHandData.Count < minCardsToFinish || botHandData.Count < minCardsToFinish)
        {
            Debug.LogWarning($"Недостаточно карт для завершения (нужно минимум {minCardsToFinish})");
            return;
        }

        if (!BotHasStood)
        {
            Debug.LogWarning("Бот ещё не завершил свои ходы.");
            return;
        }

        int playerScore = CalculateHandValue(playerHandData);
        int botScore = CalculateHandValue(botHandData);
        string winner = DetermineWinner(playerScore, botScore);

        if (casinoBank != null)
        {
            if (winner == "player")
            {
                // Победа: возврат анте (1) + выигрыш (1) = 2 фишки
                casinoBank.TryDeposit(2, OccupiedByClientId, "Win", TableType);
                Debug.Log($"[BlackGregTable] Игрок победил! Начислено 2 фишки.");
            }
            else if (winner == "draw")
            {
                // Ничья: возврат анте (1) = 1 фишка
                casinoBank.TryDeposit(1, OccupiedByClientId, "Draw", TableType);
                Debug.Log($"[BlackGregTable] Ничья! Возвращено 1 фишка.");
            }
            // Если winner == "bot" — ничего не делаем, анте уже списано
        }

        NotifyWinnerClientRpc(winner, playerScore, botScore);
        EndGame();
    }

    // ---------- RPC для запросов от игрока ----------
    [Rpc(SendTo.Server)]
    public void RequestDrawCardServerRpc(ulong clientId)
    {
        if (IsServer && IsOccupied && clientId == OccupiedByClientId)
        {
            PlayerDrawCard();
        }
    }

    /*
    [Rpc(SendTo.Server)]
    public void RequestFinishGameServerRpc(ulong clientId)
    {
        if (IsServer && IsOccupied && clientId == OccupiedByClientId)
            FinishGame();
    }
    */

    // ---------- Сетевая синхронизация рук ----------

    [ClientRpc]
    private void SyncHandsClientRpc(ulong playerClientId, CardData[] syncedPlayerHand, CardData[] syncedBotHand)
    {
        if (IsRevealed) return; 

        playerHandData = new List<CardData>(syncedPlayerHand);
        botHandData = new List<CardData>(syncedBotHand);

        CardViewCleaner(_playerSpawnedCardViews);

        placeNextCardOnLeft = true;

        // Определяем, должны ли карты игрока быть открытыми

        bool isPlayerFaceUp = (playerClientId != ulong.MaxValue);

        // Получаем руку игрока
        Transform playerHand = null;
        if (NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(playerClientId) is { } playerObj)
        {
            playerHand = FindChildByName(playerObj.transform, "CardHandPosition");
        }
        if (playerHand == null) playerHand = cardTablePosition ? cardTablePosition : playerHandParent;

        if (playerHand != null)
        {
            for (int i = 0; i < playerHandData.Count; i++)
                SpawnCardVisual(playerHandData[i], playerHand, i, isPlayerFaceUp, _playerSpawnedCardViews);
        }

        if (!BotHasStood)
        {
            CardViewCleaner(_botSpawnedCardViews);

            bool isBotFaceUp = false;

            // Получаем руку бота
            Transform botHand = null;
            if (botNetworkObjectRef.Value.TryGet(out NetworkObject botObj))
            {
                botHand = FindChildByName(botObj.transform, "CardHandPosition");
            }
            if (botHand == null) botHand = botHandParent;

            if (botHand != null)
            {
                for (int i = 0; i < botHandData.Count; i++)
                    SpawnCardVisual(botHandData[i], botHand, i, isBotFaceUp, _botSpawnedCardViews);
            }
        }
    }

    private void CardViewCleaner(List<CardView> spawnedViews)
    {
        foreach (var view in spawnedViews)
            if (view != null) Destroy(view.gameObject);
        spawnedViews.Clear();
    }

    [ClientRpc]
    private void NotifyWinnerClientRpc(string winner, int playerScore, int botScore)
    {
        Debug.Log($"Результат: Игрок {playerScore} – Бот {botScore}. Победитель: {winner}");
        // Здесь можно вызывать UnityEvent для UI
    }

    // ---------- Вспомогательные методы ----------
    /// <summary>
    /// Добавляет случайную карту в указанную руку, если не достигнут лимит.
    /// </summary>
    /// <returns>true, если карта успешно добавлена.</returns>
    private bool AddCardToHand(List<CardData> hand)
    {
        if (hand.Count >= cardLimit) return false;

        Card newCard = CardFactory.CreateRandomCard();
        hand.Add(new CardData(newCard.CardSuit, newCard.CardRank, newCard.CardType));
        return true;
    }

    private int CalculateHandValue(List<CardData> hand)
    {
        int sum = 0;
        int aceCount = 0;
        foreach (var card in hand)
        {
            int value = card.rank == CardRank.Ace ? 11 :
                        (card.rank >= CardRank.Jack ? 10 : (int)card.rank);
            sum += value;
            if (card.rank == CardRank.Ace) aceCount++;
        }
        while (sum > 21 && aceCount > 0)
        {
            sum -= 10;
            aceCount--;
        }
        return sum;
    }

    private string DetermineWinner(int playerScore, int botScore)
    {
        bool playerBust = playerScore > 21;
        bool botBust = botScore > 21;

        if (playerBust && botBust) return "draw";
        if (playerBust) return "bot";
        if (botBust) return "player";
        if (playerScore > botScore) return "player";
        if (botScore > playerScore) return "bot";
        return "draw";
    }

    private void ClearHands()
    {
        playerHandData.Clear();
        botHandData.Clear();
        CardViewCleaner(_playerSpawnedCardViews);
        CardViewCleaner(_botSpawnedCardViews);
    }

    // ---------- Визуализация ----------
    private void SpawnCardVisual(CardData cardData, Transform parent, int cardIndex, bool isFaceUp, List<CardView> spawnedViews)
    {
        if (cardViewPrefab == null) return;

        CardView view = Instantiate(cardViewPrefab, parent);
        
        view.transform.localPosition = GetNextCardPosition(cardIndex);
        view.transform.localRotation = Quaternion.Euler(startRotation);
        view.SetCardData(cardData);
        view.SetVisible(isFaceUp);
        spawnedViews.Add(view);
    }

    private Vector3 GetNextCardPosition(int currentCardIndex)
    {
        Vector3 position = startPosition;
        if (currentCardIndex == 0)
        {
            placeNextCardOnLeft = true;
            return position;
        }
        if (placeNextCardOnLeft)
        {
            int leftCount = (currentCardIndex + 1) / 2;
            position.x = startPosition.x - leftCount * spreadDistance;
            placeNextCardOnLeft = false;
        }
        else
        {
            int rightCount = (currentCardIndex + 1) / 2;
            position.x = startPosition.x + rightCount * spreadDistance;
            placeNextCardOnLeft = true;
        }
        return position;
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindChildByName(child, name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// Переопределённый метод освобождения стола игроком.
    /// После базового освобождения синхронизирует руки так, чтобы карты игрока
    /// переместились в cardTablePosition (если он задан).
    /// </summary>
    public override void Leave(ulong clientId)
    {
        base.Leave(clientId);

        if (IsServer)
        {
            SyncHandsClientRpc(ulong.MaxValue, playerHandData.ToArray(), botHandData.ToArray());
        }
    }
    /// <summary>
    /// При занятии стола игроком (возвращение) синхронизируем руки с актуальным ID,
    /// чтобы карты игрока переместились из cardTablePosition к его руке.
    /// </summary>
    public override void Occupy(ulong clientId)
    {
        base.Occupy(clientId);

        if (IsServer)
        {
            SyncHandsClientRpc(OccupiedByClientId, playerHandData.ToArray(), botHandData.ToArray());
        }
    }



    public override void OnCheaterCaught(ulong accuserClientId, bool isCheaterBot)
    {
        if (!isCheaterBot)
        {
            // Игрок-читер — пока используем поведение по умолчанию (форс-стоп).
            // В будущем здесь будет мини-игра или другая механика.
            base.OnCheaterCaught(accuserClientId, isCheaterBot);
            return;
        }

        // Бот пойман — откатываем руку и продолжаем игру.
        Debug.Log($"[BlackGregTable] Бот пойман на мухлеже игроком {accuserClientId}. Рука откачена, игра продолжается.");
        RevertBotHand();

        // Игра НЕ останавливается — бот и игрок доигрывают партию.
        // Бот продолжит свою сессию в PlaySessionRoutine, так как gameInProgress остался true.
    }

    /// <summary>
    /// Кладем карты на стол в закрытую.
    /// </summary>
    /// 
    /*
    public void PutOnTableCards(Transform cardTablePosition, List<CardData> cardsData, List<CardView> spawnedViews)
    {
        if (!IsServer) return;
        // Очистить старые визуалы
        CardViewCleaner(spawnedViews);

        if (cardTablePosition != null)
        {
            for (int i = 0; i < cardsData.Count; i++)
                SpawnCardVisual(cardsData[i], cardTablePosition, i, false, spawnedViews);
        }

        Debug.Log("[BlackGregTable] Карты на столе. Ожидание подтверждения игрока.");
    }*/

    public void PutOnTableBotCards()
    {
        if (!IsServer) return;

        PutOnTableBotCardsClientRpc();
    }

    public void PutOnTablePlayerCards()
    {
        if (!IsServer) return;
        PutOnTablePlayerCardsClientRpc();
    }

    [ClientRpc]
    public void PutOnTablePlayerCardsClientRpc()
    {
        // Очистить старые визуалы
        CardViewCleaner(_playerSpawnedCardViews);

        if (revealPlayerPosition != null)
        {
            for (int i = 0; i < playerHandData.Count; i++)
                SpawnCardVisual(playerHandData[i], revealPlayerPosition, i, false, _playerSpawnedCardViews);
        }

        Debug.Log("[BlackGregTable] Карты на столе. Ожидание подтверждения игрока.");
    }

    [ClientRpc]
    public void PutOnTableBotCardsClientRpc()
    {
        // Очистить старые визуалы
        CardViewCleaner(_botSpawnedCardViews);

        if (revealBotPosition != null)
        {
            for (int i = 0; i < botHandData.Count; i++)
                SpawnCardVisual(botHandData[i], revealBotPosition, i, false, _botSpawnedCardViews);
        }

        Debug.Log("[BlackGregTable] Карты на столе. Ожидание подтверждения игрока.");
    }

    /// <summary>
    /// Первый шаг: вскрыть карты.
    /// </summary>
    public void RevealHands()
    {
        if (!IsServer || !IsGameStarted || _isRevealed.Value) return;

        _isRevealed.Value = true;
        RevealHandsClientRpc();
        Debug.Log("[BlackGregTable] Карты вскрыты. Ожидание подтверждения игрока.");
    }

    [ClientRpc]
    private void RevealHandsClientRpc()
    {
        for (int i = 0; i < _playerSpawnedCardViews.Count; i++)
            _playerSpawnedCardViews[i].SetVisible(true);
        for (int i = 0; i < _botSpawnedCardViews.Count; i++)
            _botSpawnedCardViews[i].SetVisible(true);
    }

    /// <summary>
    /// Проверяет, может ли игрок завершить игру (для UI-подсказки).
    /// Дублируется на сервере в PlayerFinishGame для безопасности.
    /// </summary>

    public bool CanPlayerReveal()
    {
        if (!IsGameStarted) return false;
        if (_isRevealed.Value) return false;
        if (playerHandData.Count < minCardsToFinish) return false;
        if (botHandData.Count < minCardsToFinish) return false;
        if (!BotHasStood) return false;
        return true;
    }

    [Rpc(SendTo.Server)]
    public void RevealHandsServerRpc(ulong clientId)
    {
        if (IsServer && IsOccupied && clientId == OccupiedByClientId)
        {
            PutOnTablePlayerCards();
            RevealHands();
        }
    }
    public bool CanPlayerFinish()
    {
        return IsGameStarted && _isRevealed.Value;
    }


    [Rpc(SendTo.Server)]
    public void FinishGameServerRpc(ulong clientId)
    {
        if (IsServer && IsOccupied && clientId == OccupiedByClientId)
            FinishGame();
    }
}