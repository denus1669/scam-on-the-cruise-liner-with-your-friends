using Unity.Netcode;
using UnityEngine;


public class BotBluffSlapReaction : ISlapReaction<BotSlapContext>
{
    public bool CanSlap(BotSlapContext context) =>
        context.BotBehaviour != null && context.BluffController.IsBluffing;

    public void Slap(ulong slapperClientId, BotSlapContext context)
    {
        Debug.Log($"[Slap-Стратегия] Игрок {slapperClientId} купился на блеф!");

        // TODO: Логика штрафа за ложное обвинение в блефе
        if (context.DispleasureController != null)
        {
            context.DispleasureController.AddInstantDispleasureServerRpc(40f); // Мгновенный штраф недовольства
        }

        context.Router.TriggerEventClientRpc(BotSlapEventType.BluffSlapped);
    }
}