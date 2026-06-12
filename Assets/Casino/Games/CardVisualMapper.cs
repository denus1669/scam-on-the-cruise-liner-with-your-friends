using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Статический класс для сопоставления логических данных карты с координатами в текстурном атласе шейдера.
/// </summary>
public static class CardVisualMapper
{
    // Словарь, где ключ - это данные карты, а значение - вектор (X,Y) для шейдера
    private static readonly Dictionary<CardData, Vector2> VisualMap = new Dictionary<CardData, Vector2>();

    // Статический конструктор вызывается автоматически один раз при первом обращении к классу
    static CardVisualMapper()
    {
        InitializeDictionary();
    }

    private static void InitializeDictionary()
    {
        // Получаем все возможные значения из Enum
        CardSuit[] suits = (CardSuit[])Enum.GetValues(typeof(CardSuit));
        CardRank[] ranks = (CardRank[])Enum.GetValues(typeof(CardRank));
        CardType[] types = (CardType[])Enum.GetValues(typeof(CardType));

        foreach (var type in types)
        {
            foreach (var suit in suits)
            {
                foreach (var rank in ranks)
                {
                    CardData currentCard = new CardData(suit, rank, type);

                    // --- ЛОГИКА РАСЧЕТА КООРДИНАТ СЕТКИ ---
                    // Предполагается, что ваш текстурный атлас (Texture2D) - это сетка.
                    // X - зависит от номинала (от двойки до туза).
                    // Y - зависит от масти и типа карты.

                    // Ранги идут от 2 до 14. Вычитаем 2, чтобы получить индекс от 0 до 12.
                    float xIndex = (int)rank - 2;

                    // Масти идут от 0 до 3 (Черви, Буби, Крести, Пики).
                    // Тип карты смещает строку вниз (например, каждые 4 строки - новый тип).
                    float yIndex = (int)suit + ((int)type * 4);

                    Vector2 faceIndex = new Vector2(xIndex, yIndex);

                    // Добавляем в словарь
                    VisualMap[currentCard] = faceIndex;
                }
            }
        }

        // ПРИМЕЧАНИЕ: Если какая-то конкретная карта лежит в атласе не по правилам сетки, 
        // вы можете переопределить её здесь вручную. Например:
        // VisualMap[new CardData(CardSuit.Spades, CardRank.Ace, CardType.Strikethrough)] = new Vector2(10, 15);
        VisualMap[new CardData(CardSuit.Spades, CardRank.Ace, CardType.Strikethrough)] = new Vector2(10, 15);
    }

    /// <summary>
    /// Возвращает Vector2 (faceIndex) для шейдера на основе данных карты.
    /// </summary>
    public static Vector2 GetFaceIndex(CardData cardData)
    {
        if (VisualMap.TryGetValue(cardData, out Vector2 faceIndex))
        {
            return faceIndex;
        }

        Debug.LogError($"В CardVisualMapper не найдена текстура для {cardData}! Возвращен дефолтный индекс (0,0).");
        return Vector2.zero;
    }
}