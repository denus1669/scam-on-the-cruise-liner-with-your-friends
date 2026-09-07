// ==========================================
// 5. Сброс через плечо
// ==========================================
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BG_OverShoulderDrop", menuName = "Cheats/BlackGreg/5. Over Shoulder Drop")]
public class CheatOverShoulderDrop : BlackGregCheatAction
{
    public override bool CanExecute(IGameTable table, string who)
    {
        if (who == "BOT")
            return table is ICardGameTable cardTable && cardTable.GetBotScore() > 21;
        if (who == "Player")
            return table is ICardGameTable cardTable && cardTable.GetPlayerScore() > 21;
        return false;

    }

    protected override void ApplyCheat(BlackGregTable table, string who)
    {
        // Поскольку у бота БЫЛ перебор, мы делаем вид, что он выкинул лишнее, 
        // и генерируем ему безопасную руку (например, 20 очков).
        List<CardData> safeHand = new List<CardData>
        {
            new CardData(CardSuit.Clubs, CardRank.Queen, CardType.Standard),
            new CardData(CardSuit.Spades, CardRank.King, CardType.Standard)
        };


        if (who == "BOT")
        {
            table.OverwriteBotHand(safeHand);
            Debug.Log("[BlackGreg Cheats] Бот применил 'Сброс через плечо'. Перебор устранен.");
        }
        if (who == "Player")
        {
            table.OverwritePlayerHand(safeHand);
            Debug.Log("[BlackGreg Cheats] Игрок применил 'Сброс через плечо'. Перебор устранен.");
        }

    }
}