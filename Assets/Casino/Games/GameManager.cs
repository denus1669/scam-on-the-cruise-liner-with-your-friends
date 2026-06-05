/*

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("Префаб карты")]
    [SerializeField] private CardView cardViewPrefab;

    [Header("Родители для карт")]
    [SerializeField] private Transform playerHandParent;
    [SerializeField] private Transform botHandParent;

    [Header("Настройки позиции карт")]
    [SerializeField] private Vector3 startPosition = new Vector3(0.11f, 1.393f, 1.066f);
    [SerializeField] private float spreadDistance = 0.22f;

    [Header("Настройки поворота карт")]
    [SerializeField] private Vector3 startRotation = new Vector3(-30, 0, 0);  // Наклон карт "лёжа"
    [SerializeField] private float fanAngle = 15f;      // Угол веера по Y (влево-вправо)
    
    [Header("Лимит карт")]
    [SerializeField] private int cardLimit = 10;
    

    private List<CardView> playerCards = new List<CardView>();
    private List<CardView> botCards = new List<CardView>();
    private bool placeNextCardOnLeft = true;   // чередование сторон

   
    private void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            DealCardToPlayer();
        }
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            DealCardToBot();
        }
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            EvaluateRound();
    }

    public void DealCardToPlayer()
    {
        if (playerCards.Count < cardLimit)
        {
            Card newCard = CardFactory.CreateRandomCard();
            CardView view = Instantiate(cardViewPrefab, playerHandParent);

            view.transform.localPosition = GetNextCardPosition(playerCards);
            view.transform.localRotation = Quaternion.Euler(startRotation);
            view.SetCard(newCard);

            playerCards.Add(view);
            Debug.Log($"Игрок получил: {newCard}. Всего карт: {playerCards.Count}");
        }
        else Debug.Log($"Больше карт нельзя взять");

    }

    public void DealCardToBot()
    {
        if (botCards.Count < cardLimit)
        {
            Card newCard = CardFactory.CreateRandomCard();
            CardView view = Instantiate(cardViewPrefab, botHandParent);

            view.transform.localPosition = GetNextCardPosition(botCards);
            view.transform.localRotation = Quaternion.Euler(startRotation);
            view.SetCard(newCard);

            botCards.Add(view);
            Debug.Log($"Игрок получил: {newCard}. Всего карт: {botCards.Count}");
        }
        else Debug.Log($"Больше карт нельзя взять");
    }

    /// <summary>
    /// Вычисляет позицию для следующей карты с чередованием (лево/право)
    /// и обновляет флаг placeNextCardOnLeft.
    /// </summary>
    private Vector3 GetNextCardPosition(List<CardView> cardArray)
    {
        int count = cardArray.Count;
        Vector3 position = startPosition;

        if (count == 0)
        {
            // Первая карта — строго по центру, следующий флаг влево
            placeNextCardOnLeft = true;
            return position;
        }

        if (placeNextCardOnLeft)
        {
            // Карт слева от центра: (count + 1) / 2 (включая эту)
            int leftCount = (count + 1) / 2;
            position.x = startPosition.x - leftCount * spreadDistance;
            placeNextCardOnLeft = false;
        }
        else
        {
            // Карт справа от центра: (count + 2) / 2
            int rightCount = (count + 1) / 2;
            position.x = startPosition.x + rightCount * spreadDistance;
            placeNextCardOnLeft = true;
        }

        return position;
    }

    /// <summary>
    /// Подсчитывает очки руки, превращая тузы из 11 в 1 при переборе.
    /// </summary>
    private int CalculateHandValue(List<CardView> hand)
    {
        int sum = 0;
        int aceCount = 0;

        foreach (var view in hand)
        {
            ICard card = view.cardData;
            if (card == null) continue;

            sum += card.BlackGregValue;   // туз = 11, картинки = 10, остальные по номиналу
            if (card.CardRank == CardRank.Ace)
                aceCount++;
        }

        // Если перебор и есть тузы – меняем их на 1, пока сумма > 21 или не кончатся тузы
        while (sum > 21 && aceCount > 0)
        {
            sum -= 10;   // меняем одного туза с 11 на 1
            aceCount--;
        }

        return sum;
    }

    /// <summary>
    /// Оценивает раунд: подсчитывает очки, определяет победителя.
    /// </summary>
    private void EvaluateRound()
    {
        if (playerCards.Count >= 2 && botCards.Count >= 2)
        {
            int playerScore = CalculateHandValue(playerCards);
            int botScore = CalculateHandValue(botCards);

            Debug.Log($"Игрок: {playerScore} очков, Бот: {botScore} очков");

            bool playerBust = playerScore > 21;
            bool botBust = botScore > 21;

            if (playerBust && botBust)
            {
                Debug.Log("Оба перебрали! Ничья.");
            }
            else if (playerBust)
            {
                Debug.Log("Перебор у игрока. Победил бот.");
            }
            else if (botBust)
            {
                Debug.Log("Перебор у бота. Победил игрок!");
            }
            else
            {
                // Оба в игре
                if (playerScore > botScore)
                    Debug.Log("Игрок победил по очкам!");
                else if (botScore > playerScore)
                    Debug.Log("Бот победил по очкам!");
                else
                    Debug.Log("Ничья по очкам.");
            }
        }
        else Debug.Log($"У каждого должно быть минимум 2 карты. Сейчас у игрока {playerCards.Count}, а у бота {botCards.Count}");
    }



*/