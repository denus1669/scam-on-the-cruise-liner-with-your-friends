using System;
using System.Collections.Generic;
using UnityEngine;

public static class CardFactory
{
    /// <summary>
    /// Получаем массив из Enum CardSuit и CardRank.
    /// </summary>
    private static readonly CardSuit[] cardSuits = (CardSuit[])Enum.GetValues(typeof(CardSuit));
    private static readonly CardRank[] cardRanks = (CardRank[])Enum.GetValues(typeof(CardRank));

    /// <summary>
    /// Словарь, сопоставляющий типу карты его вес вероятности выпадения.
    /// Чем больше вес, тем выше шанс.
    /// </summary>
    private static readonly Dictionary<CardType, float> CardTypeWeights = new()
    {
        {CardType.Standard, 70f },
        {CardType.Cornerless, 20f },
        {CardType.Strikethrough, 10f }
    };

    public static Card CreateRandomCard()
    {
        CardSuit randomCardSuit = cardSuits[UnityEngine.Random.Range(0, cardSuits.Length)];
        CardRank randomCardRank = cardRanks[UnityEngine.Random.Range(0, cardRanks.Length)];
        CardType cardtype = GetRandomCardTypeByWeight();

        return new Card(randomCardSuit, randomCardRank, cardtype);
    }
    /// <summary>
    /// Возвращает случайный <see cref="CardType"/> на основе заданных весов.
    /// Использует алгоритм рулеточного отбора (roulette wheel selection).
    /// </summary>
    /// <returns>Выбранный тип карты.</returns>
    /// <exception cref="InvalidOperationException">Если словарь весов пуст.</exception>
    public static CardType GetRandomCardTypeByWeight()
    {
        if (CardTypeWeights.Count == 0)
            throw new InvalidOperationException("Словарь весов типов карт пуст.");

        // 1. Вычисляем общую сумму всех весов
        float totalWeight = 0;
        foreach (float weight in CardTypeWeights.Values)
            totalWeight += weight;

        // 2. Случайное число в диапазоне [0, totalWeight)
        float randomPoint = UnityEngine.Random.Range(0f, totalWeight);

        // 3. Проходим по элементам, накапливая вес, пока не достигнем случайной точки
        float cumulativeWeight = 0f;
        foreach (KeyValuePair<CardType, float> entry in CardTypeWeights)
        {
            cumulativeWeight += entry.Value;
            if (randomPoint <= cumulativeWeight)
                return entry.Key;
        }

        Debug.LogWarning("GetRandomCardByWeight: рулеточный отбор не выбрал тип карты. " +
                 $"randomPoint={randomPoint}, totalWeight={totalWeight}. " +
                 "Возвращён CardType.Standard.");
        // Сюда мы никогда не должны попасть при корректных весах и точности float,
        // но для надёжности возвращаем стандартный тип.
        return CardType.Standard;
    }
}
