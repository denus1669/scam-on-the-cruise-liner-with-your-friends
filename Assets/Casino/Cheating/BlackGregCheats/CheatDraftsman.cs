using UnityEngine;
using System.Collections.Generic;

// ==========================================
// 1. Чертежник
// ==========================================
[CreateAssetMenu(fileName = "BG_Draftsman", menuName = "Cheats/BlackGreg/1. Draftsman")]
public class CheatDraftsman : BlackGregCheatAction
{
    public override bool CanExecute(IGameTable table, string who)
    {
        if(who == "BOT")
            return table is ICardGameTable cardTable && cardTable.GetBotCardCount() > 0;
        if(who == "Player")
            return table is ICardGameTable cardTable && cardTable.GetPlayerCardCount() > 0;
        return false;
    }

    protected override void ApplyCheat(BlackGregTable table, string who)
    {
        // Бот заменяет свои карты на идеальные 21 очко (например, Туз и 10)
        List<CardData> fakePerfectHand = new List<CardData>
        {
            new CardData(CardSuit.Spades, CardRank.Ace, CardType.Standard),
            new CardData(CardSuit.Hearts, CardRank.King, CardType.Standard) // Эта карта визуально будет "перерисована" шейдером
        };
        
        if(who == "BOT")
        {
            table.OverwriteBotHand(fakePerfectHand);
            Debug.Log("[BlackGreg Cheats] Бот применил 'Чертежник'. Перерисована рука.");
        }
        if (who == "Player")
        {
            table.OverwritePlayerHand(fakePerfectHand);
            Debug.Log("[BlackGreg Cheats] Игрок применил 'Чертежник'. Перерисована рука.");
        }
    }
}