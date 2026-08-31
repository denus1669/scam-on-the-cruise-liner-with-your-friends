using System.Collections.Generic;
using UnityEngine;

// ==========================================
// 4. Матрешка
// ==========================================
[CreateAssetMenu(fileName = "BG_Matryoshka", menuName = "Cheats/BlackGreg/4. Matryoshka")]
public class CheatMatryoshka : BlackGregCheatAction
{
    public override bool CanExecute(IGameTable table, string who)
    {
        // Доступен только если у бота ровно 2 карты
        if(who == "BOT") return table is ICardGameTable cardTable && cardTable.GetBotCardCount() == 2;
        if(who == "Player") return table is ICardGameTable cardTable && cardTable.GetPlayerCardCount() == 2;
        return false;
    }

    protected override void ApplyCheat(BlackGregTable table, string who)
    {
        // Бот делает вид, что у него 2 карты, но одна из них "Толстая" (CardType.ThickDeck)
        List<CardData> fakeHand = new List<CardData>
        {
            new CardData(CardSuit.Spades, CardRank.Eight, CardType.Standard),
            //new CardData(CardSuit.Hearts, CardRank.King, CardType.ThickDeck) // Метка для толстой модели карты
        };

        if (who == "BOT")
        {
            table.OverwriteBotHand(fakeHand);
            Debug.Log("[BlackGreg Cheats] Бот применил 'Матрешку'.");
        }
        if (who == "Player")
        {
            table.OverwritePlayerHand(fakeHand);
            Debug.Log("[BlackGreg Cheats] Игрок применил 'Матрешку'.");
        }
    }
}