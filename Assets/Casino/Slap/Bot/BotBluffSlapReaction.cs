using Unity.Netcode;
using UnityEngine;


public class BotBluffSlapReaction : NetworkBehaviour, ISlapReaction<BotSlapContext>
{
    public bool CanSlap(BotSlapContext context) =>
        context.BotBehavior != null && context.BotBehavior.IsBotBluffingActive;

    public void Slap(ulong slapperClientId, BotSlapContext context)
    {
        Debug.Log($"[Slap-Стратегия] Игрок {slapperClientId} купился на блеф!");
        // TODO: Логика штрафа за ложное обвинение в блефе
        if (context.DispleasureController != null)
        {
            context.DispleasureController.AddInstantDispleasure(50f); // Мгновенный штраф недовольства
        }

        context.Router.TriggerEventClientRpc(BotSlapEventType.BluffSlapped);
    }
}