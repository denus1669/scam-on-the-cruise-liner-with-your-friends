using System;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public struct CardData : INetworkSerializable, IEquatable<CardData>
{
    public CardSuit suit;
    public CardRank rank;
    public CardType type;

    public CardData(CardSuit suit, CardRank rank, CardType type)
    {
        this.suit = suit;
        this.rank = rank;
        this.type = type;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref suit);
        serializer.SerializeValue(ref rank);
        serializer.SerializeValue(ref type);
    }

    public override string ToString() => $"{rank} of {suit} [{type}]";

    // --- Методы ниже нужны, чтобы использовать структуру как ключ в Dictionary ---

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
        // Создаем уникальный хэш на основе всех трех значений
        return HashCode.Combine((int)suit, (int)rank, (int)type);
    }

}