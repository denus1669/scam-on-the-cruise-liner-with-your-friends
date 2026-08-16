using UnityEngine;
using System.Collections.Generic;

// ==========================================
// 2. Веер карт
// ==========================================

[CreateAssetMenu(fileName = "BG_CardFan", menuName = "Cheats/BlackGreg/2. Card Fan")]
public class CheatCardFan : BlackGregBotCheatAction
{
    public override bool CanExecute(IGameTable table)
    {
        return table is ICardGameTable;
    }

    protected override void ApplyBotCheat(BlackGregTable table)
    {
        // Абсурдная рука: 9 двоек и одна тройка = 21 очко
        List<CardData> absurdHand = new List<CardData>();
        for (int i = 0; i < 9; i++)
        {
            absurdHand.Add(new CardData((CardSuit)(i % 4), CardRank.Two, CardType.Standard));
        }
        absurdHand.Add(new CardData(CardSuit.Clubs, CardRank.Three, CardType.Standard));

        table.OverwriteBotHand(absurdHand);
        Debug.Log("[BlackGreg Cheats] Бот применил 'Веер карт'. Вывалена куча макулатуры на 21 очко.");
    }
}