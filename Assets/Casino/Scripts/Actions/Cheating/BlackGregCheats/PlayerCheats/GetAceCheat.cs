using UnityEngine;
using System.Collections.Generic;
using Assets.Casino.Games;
using Assets.Casino.Games.BlackGreg;

namespace Assets.Casino.Cheating.BlackGregCheats
{

    // ==========================================
    // 2. Веер карт
    // ==========================================

    [CreateAssetMenu(fileName = "BG_GetAceCheat", menuName = "Cheats/BlackGreg/PlayerCheats/1. GetAceCheat")]
    public class GetAceCheat : BlackGregCheatAction
    {
        public override bool CanExecute(IGameTable table, string who)
        {
            return table is ICardGameTable;
        }

        protected override void ApplyCheat(BlackGregTable table, string who)
        {
            // Абсурдная рука: 9 двоек и одна тройка = 21 очко
            CardData aceCard = new CardData(CardFactory.CreateRandomSuit(), CardRank.Ace, CardFactory.CreateRandomType());


            if (who == "BOT")
            {
                table.AddCardToHandCheats(table.GetBotHandData(), aceCard);
                Debug.Log("[BlackGreg Cheats] Бот применил 'Получить Туз'.");
            }
            if (who == "Player")
            {
                table.AddCardToHandCheats(table.GetPlayerHandData(), aceCard);
                Debug.Log("[BlackGreg Cheats] Игрок применил 'Получить Туз'.");
            }
        }
    }
}