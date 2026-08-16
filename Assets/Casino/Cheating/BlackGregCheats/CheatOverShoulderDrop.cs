// ==========================================
// 5. Сброс через плечо
// ==========================================
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BG_OverShoulderDrop", menuName = "Cheats/BlackGreg/5. Over Shoulder Drop")]
public class CheatOverShoulderDrop : BlackGregBotCheatAction
{
    public override bool CanExecute(IGameTable table)
    {
        return table is ICardGameTable cardTable && cardTable.GetBotScore() > 21;
    }

    protected override void ApplyBotCheat(BlackGregTable table)
    {
        // Поскольку у бота БЫЛ перебор, мы делаем вид, что он выкинул лишнее, 
        // и генерируем ему безопасную руку (например, 20 очков).
        List<CardData> safeHand = new List<CardData>
        {
            new CardData(CardSuit.Clubs, CardRank.Queen, CardType.Standard),
            new CardData(CardSuit.Spades, CardRank.King, CardType.Standard)
        };

        table.OverwriteBotHand(safeHand);
        Debug.Log("[BlackGreg Cheats] Бот применил 'Сброс через плечо'. Перебор устранен.");
    }
}