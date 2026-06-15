using UnityEngine;
using System.Collections.Generic;

// ==========================================
// 1. Чертежник
// ==========================================
[CreateAssetMenu(fileName = "BG_Draftsman", menuName = "Cheats/BlackGreg/1. Draftsman")]
public class CheatDraftsman : BlackGregCheatAction
{
    public override bool CanExecute(CheatContext context)
    {
        var table = context.GetTableAs<ICardGameTable>();
        return table != null && table.GetBotCardCount() > 0;
    }

    protected override void ApplyBotCheat(BlackGregTable table, BotAgent bot)
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

    protected override void ApplyPlayerCheat(BlackGregTable table, ulong playerId)
    {
        // TODO: Реализация для игрока. Нужно позволить игроку выбрать карту и изменить ее номинал.
    }
}