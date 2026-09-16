using Assets.Casino.Slap;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Slap.Bot
{
    public class BotIdleSlapReaction : ISlapReaction<BotSlapContext>
    {
        public bool CanSlap(BotSlapContext context) => true;

        public void Slap(ulong slapperClientId, BotSlapContext context)
        {
            Debug.Log($"[Slap-Стратегия] Игрок {slapperClientId} ударил бота без причины!");

            if (context.DispleasureController != null)
            {
                context.DispleasureController.AddInstantDispleasureServerRpc(40f); // Мгновенный штраф недовольства
            }

            context.Router.TriggerEventClientRpc(BotSlapEventType.IdleSlapped);
        }
    }
}