using UnityEngine;
using System.Collections.Generic;

// ==========================================
// 1. Чертежник
// ==========================================
[CreateAssetMenu(fileName = "BG_Draftsman", menuName = "Cheats/BlackGreg/1. Draftsman")]
public class CheatDraftsman : BlackGregBotCheatAction
{
    public override bool CanExecute(IGameTable table)
    {
        return table is ICardGameTable cardTable && cardTable.GetBotCardCount() > 0;
    }

    protected override void ApplyBotCheat(BlackGregTable table)
    {
        // Бот заменяет свои карты на идеальные 21 очко (например, Туз и 10)
        List<CardData> fakePerfectHand = new List<CardData>
        {
            new CardData(CardSuit.Spades, CardRank.Ace, CardType.Standard),
            new CardData(CardSuit.Hearts, CardRank.King, CardType.Standard) // Эта карта визуально будет "перерисована" шейдером
        };

        table.OverwriteBotHand(fakePerfectHand);
        Debug.Log("[BlackGreg Cheats] Бот применил 'Чертежник'. Рука заменена на 21.");
    }
}