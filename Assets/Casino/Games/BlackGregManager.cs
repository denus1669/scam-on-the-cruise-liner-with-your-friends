using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Управляет логикой стола. Данные (ICard) отделены от визуала (CardView).
/// Подготовлено для переноса логики на сервер (NGO).
/// </summary>
public class BlackGregManager : MonoBehaviour
{
    [Header("Префаб карты")]
    [SerializeField] private CardView cardViewPrefab;

    [Header("Руки (визуальные контейнеры)")]
    [SerializeField] private Transform playerHandParent;
    [SerializeField] private Transform botHandParent;

    [Header("Настройки позиционирования")]
    [SerializeField] private Vector3 startPosition = new Vector3(0.11f, 1.393f, 1.066f);
    [SerializeField] private float spreadDistance = 0.22f;
    [SerializeField] private Vector3 startRotation = new Vector3(-30, 0, 0);

    [Header("Настройки игры")]
    [SerializeField] private int cardLimit = 10;
    [SerializeField] private int minCardsToFinish = 2;


    // ЧИСТЫЕ ДАННЫЕ (Будут работать на сервере)
    private List<ICard> playerHandData = new List<ICard>();
    private List<ICard> botHandData = new List<ICard>();

    // ВИЗУАЛ (Только для клиента: отрисовка и удаление моделей)
    private List<CardView> spawnedCardViews = new List<CardView>();
    private bool placeNextCardOnLeft = true;

    // События для связи с экономикой (GameManager)
    public UnityEvent<int> OnPlayerWon;
    public UnityEvent OnBotWon;
    public UnityEvent OnDraw;




    public void PlayerDrawCard()
    {
        DealCard(playerHandData, playerHandParent, "Игрок");
    }

    public void BotDrawCard()
    {
        DealCard(botHandData, botHandParent, "Бот");
    }

    public void FinishGame()
    {
        if (playerHandData.Count < minCardsToFinish || botHandData.Count < minCardsToFinish)
        {
            Debug.Log($"Нужно минимум {minCardsToFinish} карты для завершения!");
            return;
        }

        EvaluateRound();
    }

    private void DealCard(List<ICard> handData, Transform parentTransform, string logName)
    {
        if (handData.Count >= cardLimit) return;

        // 1. Логика (Данные)
        Card newCard = CardFactory.CreateRandomCard();
        handData.Add(newCard);

        // 2. Визуал (Отображение)
        SpawnCardVisual(newCard, parentTransform, handData.Count - 1);

        Debug.Log($"{logName} получил: {newCard}. Всего карт: {handData.Count}");
    }

    private void SpawnCardVisual(ICard cardData, Transform parent, int cardIndex)
    {
        CardView view = Instantiate(cardViewPrefab, parent);
        view.transform.localPosition = GetNextCardPosition(cardIndex);
        view.transform.localRotation = Quaternion.Euler(startRotation);

        spawnedCardViews.Add(view); // Сохраняем только чтобы потом удалить (Destroy)
    }

    // Теперь зависит только от индекса (числа), а не от UI-объектов
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

    // Расчет идет ИСКЛЮЧИТЕЛЬНО по чистым интерфейсам ICard
    private int CalculateHandValue(List<ICard> handData)
    {
        int sum = 0;
        int aceCount = 0;

        foreach (var card in handData)
        {
            sum += card.BlackGregValue;
            if (card.CardRank == CardRank.Ace)
                aceCount++;
        }

        while (sum > 21 && aceCount > 0)
        {
            sum -= 10;
            aceCount--;
        }

        return sum;
    }

    private void EvaluateRound()
    {
        // Передаем списки с данными
        int playerScore = CalculateHandValue(playerHandData);
        int botScore = CalculateHandValue(botHandData);

        Debug.Log($"Итог: Игрок {playerScore} | Бот {botScore}");

        bool playerBust = playerScore > 21;
        bool botBust = botScore > 21;

        if (playerBust && botBust) OnDraw?.Invoke();
        else if (playerBust) OnBotWon?.Invoke();
        else if (botBust) OnPlayerWon?.Invoke(1);
        else
        {
            if (playerScore > botScore) OnPlayerWon?.Invoke(1);
            else if (botScore > playerScore) OnBotWon?.Invoke();
            else OnDraw?.Invoke();
        }

        ClearHands();
    }

    private void ClearHands()
    {
        // Очищаем визуал
        foreach (var view in spawnedCardViews)
        {
            if (view != null) Destroy(view.gameObject);
        }
        spawnedCardViews.Clear();

        // Очищаем данные
        playerHandData.Clear();
        botHandData.Clear();
    }
}