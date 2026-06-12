using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

/// <summary>
/// Управляет логикой стола. Сетевая версия.
/// </summary>
public class BlackGregManager : NetworkBehaviour
{
    [Header("Префаб карты")]
    [SerializeField] private CardView cardViewPrefab;

    [Header("Руки (визуальные контейнеры на столе)")]
    [SerializeField] private Transform playerHandParent;
    [SerializeField] private Transform botHandParent;
    [SerializeField] private Transform cardTablePosition; // Позиция для сброшенных/оставленных карт


    [Header("Настройки позиционирования")]
    [SerializeField] private Vector3 startPosition = new Vector3(0.11f, 1.393f, 1.066f);
    [SerializeField] private float spreadDistance = 0.22f;
    [SerializeField] private Vector3 startRotation = new Vector3(30, 180, 0);

    [Header("Настройки игры")]
    [SerializeField] private int cardLimit = 10;
    [SerializeField] private int minCardsToFinish = 2;
    [SerializeField] public NetworkVariable<bool> gameInProgress = new NetworkVariable<bool>(false);

    [Header("Ссылка на стул")]
    [SerializeField] private TableInteractable tableInteractable;

    [Header("Бот")]
    [SerializeField] private bool botHasStood = false;

    public TableInteractable TableInteractable => tableInteractable;

    // Данные рук
    private List<CardData> playerHandData = new List<CardData>();
    private List<CardData> botHandData = new List<CardData>();

    // Визуал (только на клиенте)
    private List<CardView> spawnedCardViews = new List<CardView>();
    private bool placeNextCardOnLeft = true;

    // События для UI (вызываются на всех клиентах)
    public UnityEvent<int> OnPlayerWon;
    public UnityEvent OnBotWon;
    public UnityEvent OnDraw;

    public int GetBotScore() => CalculateHandValue(botHandData);
    public int GetBotCardCount() => botHandData.Count;
    public List<CardData> GetBotHandCopy() => new List<CardData>(botHandData);


    #region Server-only logic
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Подписываемся на смену владельца стола, чтобы двигать карты
        if (tableInteractable != null)
        {
            tableInteractable.OccupiedByClientIdVar.OnValueChanged += HandleOccupantChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (tableInteractable != null)
        {
            tableInteractable.OccupiedByClientIdVar.OnValueChanged -= HandleOccupantChanged;
        }
    }

    private void HandleOccupantChanged(ulong oldId, ulong newId)
    {
        if (IsServer)
        {
            // Владелец сменился (кто-то сел за стол или ушел из триггера)
            // Заставляем всех клиентов перерисовать позиции и состояния карт
            SyncHandsClientRpc(newId, playerHandData.ToArray(), botHandData.ToArray());
        }
    }

    private void PlayerDrawCard()
    {
        if (!IsServer) return;
        if (playerHandData.Count >= cardLimit) return;
        if (playerHandData.Count == 1) gameInProgress.Value = true;

        Card newCard = CardFactory.CreateRandomCard();
        playerHandData.Add(new CardData(newCard.CardSuit, newCard.CardRank, newCard.CardType));

        // Передаем обновленные списки всем клиентам в виде массивов
        SyncHandsClientRpc(tableInteractable.GetOccupyingClientId(), playerHandData.ToArray(), botHandData.ToArray());
    }
    public void BotStand()
    {
        if (!IsServer) return;
        botHasStood = true; // флаг, что бот закончил
                            // Не завершаем игру, только ждём, пока игрок нажмёт Finish
    }

    public void BotDrawCard()
    {
        if (!IsServer) return;
        if (botHandData.Count >= cardLimit) return;

        Card newCard = CardFactory.CreateRandomCard();
        botHandData.Add(new CardData(newCard.CardSuit, newCard.CardRank, newCard.CardType));

        // Передаем обновленные списки всем клиентам
        SyncHandsClientRpc(tableInteractable.GetOccupyingClientId(), playerHandData.ToArray(), botHandData.ToArray());
    }

    private void FinishGame()
    {
        if (!IsServer) return;

        if (playerHandData.Count < minCardsToFinish || botHandData.Count < minCardsToFinish)
        {
            Debug.Log($"Нужно минимум {minCardsToFinish} карт для завершения!");
            return;
        }

        if (!botHasStood)
        {
            Debug.Log($"Бот еще не завершил своих ходы!");
            return;

        }

        int playerScore = CalculateHandValue(playerHandData);
        int botScore = CalculateHandValue(botHandData);

        Debug.Log($"Итог: Игрок {playerScore} | Бот {botScore}");

        string winner = DetermineWinner(playerScore, botScore);
        NotifyWinnerClientRpc(winner, playerScore, botScore);

        ClearHands();
        gameInProgress.Value = false;
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

    public int CalculateHandValue(List<CardData> handData)
    {
        int sum = 0;
        int aceCount = 0;
        foreach (var card in handData)
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

    private void ClearHands()
    {
        playerHandData.Clear();
        botHandData.Clear();
        // Отправляем пустые массивы, чтобы очистить стол у всех
        SyncHandsClientRpc(tableInteractable.GetOccupyingClientId(), playerHandData.ToArray(), botHandData.ToArray());
    }

    #endregion

    #region RPCs

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestDrawCardServerRpc(ulong clientId)
    {
        if (!IsServer) return;
        if (tableInteractable != null && tableInteractable.IsOccupied() && clientId == tableInteractable.GetOccupyingClientId())
        {
            PlayerDrawCard();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestFinishGameServerRpc(ulong clientId)
    {
        if (!IsServer) return;
        if (tableInteractable != null && tableInteractable.IsOccupied() && clientId == tableInteractable.GetOccupyingClientId())
        {
            FinishGame();
        }
    }

    // ТЕПЕРЬ RPC ПРИНИМАЕТ МАССИВЫ КАРТ ОТ СЕРВЕРА
    [ClientRpc]
    private void SyncHandsClientRpc(ulong playerClientId, CardData[] syncedPlayerHand, CardData[] syncedBotHand)
    {
        playerHandData = new List<CardData>(syncedPlayerHand);
        botHandData = new List<CardData>(syncedBotHand);

        foreach (var view in spawnedCardViews)
            if (view != null) Destroy(view.gameObject);
        spawnedCardViews.Clear();
        placeNextCardOnLeft = true;

        bool isTableOccupied = playerClientId != ulong.MaxValue;

        Transform playerHand = null;
        if (isTableOccupied)
        {
            playerHand = FindCardHandPositionForPlayer(playerClientId);
        }

        // Если стол свободен ИЛИ рука не найдена - карты лежат на столе
        if (playerHand == null)
        {
            playerHand = cardTablePosition != null ? cardTablePosition : playerHandParent;
        }

        Transform botHand = FindCardHandPositionForBot() ?? botHandParent;

        // Создаем карты. isTableOccupied определяет, лицом вверх (true) или рубашкой (false)
        for (int i = 0; i < playerHandData.Count; i++)
            SpawnCardVisual(playerHandData[i], playerHand, i, isTableOccupied);

        if (botHand != null)
        {
            for (int i = 0; i < botHandData.Count; i++)
                SpawnCardVisual(botHandData[i], botHand, i, isTableOccupied);
        }
    }
    private Transform FindCardHandPositionForPlayer(ulong clientId)
    {
        // Если ID указывает на отсутствие игрока, сразу возвращаем null
        if (clientId == ulong.MaxValue) return null;

        foreach (var networkObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (networkObj.IsPlayerObject && networkObj.OwnerClientId == clientId)
            {
                Transform hand = FindCardHandPosition(networkObj.transform);
                if (hand != null) return hand;
            }
        }
        return null;
    }

    private Transform FindCardHandPositionForBot()
    {
        GameObject[] botObjs = GameObject.FindGameObjectsWithTag("Bot");
        foreach (var botObj in botObjs)
        {
            Transform hand = FindCardHandPosition(botObj.transform);
            if (hand != null) return hand;
        }

        if (botHandParent != null) return botHandParent;

        Debug.LogWarning("Рука бота не найдена ни по тегу, ни в Инспекторе.");
        return null;
    }


    [ClientRpc]
    private void NotifyWinnerClientRpc(string winner, int playerScore, int botScore)
    {
        switch (winner)
        {
            case "player": OnPlayerWon?.Invoke(playerScore); break;
            case "bot": OnBotWon?.Invoke(); break;
            default: OnDraw?.Invoke(); break;
        }
        Debug.Log($"Результат: Player {playerScore} – Bot {botScore}. Winner: {winner}");
    }

    #endregion

    #region Visual Helpers

    private void SpawnCardVisual(CardData cardData, Transform parent, int cardIndex, bool isFaceUp)
    {
        CardView view = Instantiate(cardViewPrefab, parent);
        view.transform.localPosition = GetNextCardPosition(cardIndex);
        view.transform.localRotation = Quaternion.Euler(startRotation);
        view.SetCardData(cardData);
        view.SetVisible(isFaceUp); // Устанавливаем статус скрытости
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

    private Transform FindCardHandPosition(Transform root)
    {
        if (root.name == "CardHandPosition") return root;
        foreach (Transform child in root)
        {
            var result = FindCardHandPosition(child);
            if (result != null) return result;
        }
        return null;
    }

    #endregion
}