using UnityEngine;

public enum PurchaseScope
{
    Individual, // каждый экземпляр покупается отдельно
    Global      // покупка одной группы открывает все её экземпляры
}

[CreateAssetMenu(fileName = "PurchaseItem", menuName = "Casino/Purchase Item")]
public class PurchaseItemDefinition : ScriptableObject
{
    [Tooltip("Уникальный идентификатор предмета")]
    public string itemId;

    [Tooltip("Отображаемое название")]
    public string displayName;

    [Tooltip("Цена")]
    public int price = 100;

    [Tooltip("Individual: покупается каждый экземпляр. Global: покупка открывает всю группу")]
    public PurchaseScope scope = PurchaseScope.Individual;

    [Tooltip("Для Global: общий ключ группы. Например 'cheat_swap_card'")]
    public string groupId;
}