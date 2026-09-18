namespace Assets.Casino.Games.BlackGreg
{
    public enum CardRank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }
    public enum CardSuit
    {
        Hearts,
        Diamonds,
        Clubs,
        Spades
    }
    public enum CardType
    {
        Standard,   // Обычная карта
        Strikethrough,   // Перечеркнутая карта
        Cornerless,   // Безуголковая карта
    }

    public interface ICard
    {
        int BlackGregValue { get; }
        CardSuit CardSuit { get; }
        CardRank CardRank { get; }
        CardType CardType { get; }
    }

    public class Card : ICard
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
}