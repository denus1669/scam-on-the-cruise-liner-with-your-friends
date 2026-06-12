using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public struct CardData : INetworkSerializable
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
}