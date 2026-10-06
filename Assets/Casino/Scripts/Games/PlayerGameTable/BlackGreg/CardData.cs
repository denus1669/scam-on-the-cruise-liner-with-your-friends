using System;
using Unity.Netcode;

namespace Assets.Casino.Games.BlackGreg
{
    [System.Serializable]
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

    public struct CardData : INetworkSerializable, IEquatable<CardData>
    {
        public CardSuit suit;
        public CardRank rank;
        public CardType type;

        public CardData(CardSuit suit, CardRank cardRank, CardType type)
        {
            this.suit = suit;
            this.rank = cardRank;
            this.type = type;
        }

        /// <summary>
        /// Базовое значение карты для подсчета очков.
        /// Туз = 11, Картинки (J, Q, K) = 10, остальные по номиналу.
        /// Примечание: Логика уменьшения значения Туза до 1 при переборе (>21) 
        /// должна обрабатываться на уровне подсчета всей руки (CalculateHandValue).
        /// </summary>
        public int BaseValue => rank switch
        {
            CardRank.Ace => 11,
            CardRank.Jack or CardRank.Queen or CardRank.King => 10,
            _ => (int)rank
        };

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref suit);
            serializer.SerializeValue(ref rank);
            serializer.SerializeValue(ref type);
        }

        public override string ToString() => $"{rank} of {suit} [{type}]";

        public bool Equals(CardData other)
        {
            return suit == other.suit && rank == other.rank && type == other.type;
        }

        public override bool Equals(object obj)
        {
            return obj is CardData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)suit, (int)rank, (int)type);
        }
    }
}