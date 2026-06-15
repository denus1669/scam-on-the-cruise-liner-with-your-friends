// ==========================================
// 3. Карманный обмен
// ==========================================
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BG_PocketSwap", menuName = "Cheats/BlackGreg/3. Pocket Swap")]
public class CheatPocketSwap : BlackGregCheatAction
{
    public override bool CanExecute(CheatContext context)
    {
        return context.GetTableAs<ICardGameTable>() != null;
    }

    protected override void ApplyBotCheat(BlackGregTable table, BotAgent bot)
    {
        // Бот достает идеальные карты, но одна из них - из чужой колоды (CardType.Foreign)
        List<CardData> fakeHand = new List<CardData>
        {
            new CardData(CardSuit.Diamonds, CardRank.Jack, CardType.Standard),
            //new CardData(CardSuit.Clubs, CardRank.Ace, CardType.Foreign) // Foreign - метка для шейдера (другая рубашка)
        };

        table.OverwriteBotHand(fakeHand);
        Debug.Log("[BlackGreg Cheats] Бот применил 'Карманный обмен'. Рубашка одной карты палевная.");
    }

    protected override void ApplyPlayerCheat(BlackGregTable table, ulong playerId)
    {
        // TODO: Логика для игрока
    }
}