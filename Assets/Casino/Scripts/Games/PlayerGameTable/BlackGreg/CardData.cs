using System;
using Unity.Netcode;

namespace Assets.Casino.Games.BlackGreg
{
    [System.Serializable]
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