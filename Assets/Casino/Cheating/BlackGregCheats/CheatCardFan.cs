using UnityEngine;
using System.Collections.Generic;

// ==========================================
// 2. Веер карт
// ==========================================

[CreateAssetMenu(fileName = "BG_CardFan", menuName = "Cheats/BlackGreg/2. Card Fan")]
public class CheatCardFan : BlackGregCheatAction
{
    public override bool CanExecute(IGameTable table, string who)
    {
        return table is ICardGameTable;
    }

    protected override void ApplyCheat(BlackGregTable table, string who)
    {
        // Абсурдная рука: 9 двоек и одна тройка = 21 очко
        List<CardData> absurdHand = new List<CardData>();
        for (int i = 0; i < 9; i++)
        {
            absurdHand.Add(new CardData((CardSuit)(i % 4), CardRank.Two, CardType.Standard));
        }
        absurdHand.Add(new CardData(CardSuit.Clubs, CardRank.Three, CardType.Standard));
        

        if (who == "BOT")
        {
            table.OverwriteBotHand(absurdHand);
            Debug.Log("[BlackGreg Cheats] Бот применил 'Веер карт'. Вывалена куча макулатуры на 21 очко.");
        }
        if (who == "Player")
        {
            table.OverwritePlayerHand(absurdHand);
            Debug.Log("[BlackGreg Cheats] Игрок применил 'Веер карт'. Вывалена куча макулатуры на 21 очко.");
        }
    }
}