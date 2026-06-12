public interface ICard
{
    int BlackGregValue { get; }
    CardSuit CardSuit { get; }
    CardRank CardRank { get; }
    CardType CardType { get; }
}