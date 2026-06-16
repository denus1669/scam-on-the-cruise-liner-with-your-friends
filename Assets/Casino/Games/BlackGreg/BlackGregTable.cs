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

    [Header("Positioning")]
    [SerializeField] private Vector3 startPosition = new Vector3(0.11f, 1.393f, 1.066f);
    [SerializeField] private float spreadDistance = 0.22f;
    [SerializeField] private Vector3 startRotation = new Vector3(30, 180, 0);

    [Header("Game Rules")]
    [SerializeField] private int cardLimit = 10;
    [SerializeField] private int minCardsToFinish = 2;

    // Данные рук (сервер)
    private List<CardData> playerHandData = new List<CardData>();
    private List<CardData> botHandData = new List<CardData>();

    // Визуал на клиентах
    private List<CardView> spawnedCardViews = new List<CardView>();
    private bool placeNextCardOnLeft = true;

    // Состояние бота
    private bool botHasStood = false;

    // Кэшированные ссылки на NetworkObject игрока и бота
    private NetworkObject playerNetworkObject;
    private NetworkObject botNetworkObject;

    // ---------- Реализация ICardGameTable ----------
    public int GetBotScore() => CalculateHandValue(botHandData);
    public int GetPlayerScore() => CalculateHandValue(playerHandData);
    public int GetBotCardCount() => botHandData.Count;

    public List<CardData> GetBotHandCopy()
    {
        return new List<CardData>(botHandData);
    }

    public void BotDrawCard()
    {
        if (!IsServer || !IsGameStarted) return;

        if (AddCardToHand(botHandData))
        {
            SyncHandsClientRpc(OccupiedByClientId, playerHandData.ToArray(), botHandData.ToArray());
        }
    }

    public void BotStand()
    {
        if (!IsServer) return;
        botHasStood = true;
    }

    // ---------- Переопределение жизненного цикла ----------
    public override void StartGame()
    {
        if (!IsServer || !CanStartGame()) return;

        // Сброс состояния для новой игры
        playerHandData.Clear();
        botHandData.Clear();
        botHasStood = false;

        base.StartGame(); // устанавливает gameInProgress.Value = true
    }

    public override void EndGame()
    {
        if (!IsServer || !IsGameStarted) return;

        base.EndGame(); // устанавливает gameInProgress.Value = false
        ClearHands();   // очистка визуала и данных
    }

    // ---------- Методы для игрока (вызываются UI) ----------
    public void PlayerDrawCard()
    {
        if (!IsServer || !IsGameStarted) return;

        if (AddCardToHand(playerHandData))
        {
            SyncHandsClientRpc(OccupiedByClientId, playerHandData.ToArray(), botHandData.ToArray());
        }
    }

    public void PlayerFinishGame()
    {
        if (!IsServer || !IsGameStarted) return;

        if (playerHandData.Count < minCardsToFinish || botHandData.Count < minCardsToFinish)
        {
            Debug.LogWarning($"Недостаточно карт для завершения (нужно минимум {minCardsToFinish})");
            return;
        }

        if (!botHasStood)
        {
            Debug.LogWarning("Бот ещё не завершил свои ходы.");
            return;
        }

        int playerScore = CalculateHandValue(playerHandData);
        int botScore = CalculateHandValue(botHandData);
        string winner = DetermineWinner(playerScore, botScore);

        NotifyWinnerClientRpc(winner, playerScore, botScore);
        EndGame();
    }

    // ---------- RPC для запросов от игрока ----------
    [Rpc(SendTo.Server)]
    public void RequestDrawCardServerRpc(ulong clientId)
    {
        if (IsServer && IsOccupied && clientId == OccupiedByClientId)
            PlayerDrawCard();
    }

    [Rpc(SendTo.Server)]
    public void RequestFinishGameServerRpc(ulong clientId)
    {
        if (IsServer && IsOccupied && clientId == OccupiedByClientId)
            PlayerFinishGame();
    }

    // ---------- Сетевая синхронизация рук ----------
    [ClientRpc]
    private void SyncHandsClientRpc(ulong playerClientId, CardData[] syncedPlayerHand, CardData[] syncedBotHand)
    {
        playerHandData = new List<CardData>(syncedPlayerHand);
        botHandData = new List<CardData>(syncedBotHand);

        // Очистка старых визуалов
        foreach (var view in spawnedCardViews)
            if (view != null) Destroy(view.gameObject);
        spawnedCardViews.Clear();

        placeNextCardOnLeft = true;

        Transform playerHand = GetHandTransform(playerNetworkObject);
        if (playerHand == null) playerHand = cardTablePosition ? cardTablePosition : playerHandParent;

        if (playerHand != null)
        {
            for (int i = 0; i < playerHandData.Count; i++)
                SpawnCardVisual(playerHandData[i], playerHand, i, true);
        }

        Transform botHand = GetHandTransform(botNetworkObject);
        if (botHand == null) botHand = botHandParent;

        if (botHand != null)
        {
            for (int i = 0; i < botHandData.Count; i++)
                SpawnCardVisual(botHandData[i], botHand, i, true);
        }
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
        SyncHandsClientRpc(OccupiedByClientId, playerHandData.ToArray(), botHandData.ToArray());
    }

    // ---------- Визуализация ----------
    private void SpawnCardVisual(CardData cardData, Transform parent, int cardIndex, bool isFaceUp)
    {
        if (cardViewPrefab == null) return;

        CardView view = Instantiate(cardViewPrefab, parent);
        view.transform.localPosition = GetNextCardPosition(cardIndex);
        view.transform.localRotation = Quaternion.Euler(startRotation);
        view.SetCardData(cardData);
        view.SetVisible(isFaceUp);
        spawnedCardViews.Add(view);
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

    /// <summary>
    /// Ищет дочерний Transform с именем "CardHandPosition" внутри указанного сетевого объекта.
    /// </summary>
    private Transform GetHandTransform(NetworkObject netObj)
    {
        if (netObj == null) return null;
        return FindChildByName(netObj.transform, "CardHandPosition");
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
}