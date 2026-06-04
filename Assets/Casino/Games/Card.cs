using UnityEngine;

public class Card: ICard
{
    public CardSuit CardSuit { get; set; }

    public CardRank CardRank { get; set; }

    public CardType CardType { get; set; }

    // Базовое значение туз = 11, картинки = 10, остальные по номиналу.
    public int BlackGregValue => CardRank switch
    {
        CardRank.Ace => 11,
        CardRank.Jack or CardRank.Queen or CardRank.King => 10,
        _ => (int)CardRank
    };

    public Card(CardSuit cardSuit, CardRank cardRank, CardType cardType) 
    {
        CardSuit = cardSuit;
        CardRank = cardRank;
        CardType = cardType;
    }

    public override string ToString() => $"{CardRank} of {CardSuit} [{CardType}]";
}
