using System.Collections.Generic;
using UnityEngine;

// ==========================================
// 4. Матрешка
// ==========================================
[CreateAssetMenu(fileName = "BG_Matryoshka", menuName = "Cheats/BlackGreg/4. Matryoshka")]
public class CheatMatryoshka : BlackGregCheatAction
{
    public override bool CanExecute(CheatContext context)
    {
        var table = context.GetTableAs<ICardGameTable>();
        // Доступен только если у бота ровно 2 карты
        return table != null && table.GetBotCardCount() == 2;
    }

    protected override void ApplyBotCheat(BlackGregTable table, BotAgent bot)
    {
        // Бот делает вид, что у него 2 карты, но одна из них "Толстая" (CardType.ThickDeck)
        List<CardData> fakeHand = new List<CardData>
        {
            new CardData(CardSuit.Spades, CardRank.Eight, CardType.Standard),
            //new CardData(CardSuit.Hearts, CardRank.King, CardType.ThickDeck) // Метка для толстой модели карты
        };

        table.OverwriteBotHand(fakeHand);
        Debug.Log("[BlackGreg Cheats] Бот применил 'Матрешку'.");
    }

    protected override void ApplyPlayerCheat(BlackGregTable table, ulong playerId)
    {
        // TODO: Логика для игрока
    }
}