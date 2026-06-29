using Unity.Netcode;
using UnityEngine;

public class BotCheatSlapReaction : ISlapReaction<BotSlapContext>
{
    public bool CanSlap(BotSlapContext context) =>
           context.CheatController != null && context.CheatController.IsCheating;

    public void Slap(ulong slapperClientId, BotSlapContext context)
    {
        Debug.Log($"[Slap-Стратегия] Игрок {slapperClientId} ПОЙМАЛ бота на мухлеже!");
        context.CheatController.HandleCheatCaught(slapperClientId);
        context.Router.TriggerEventClientRpc(BotSlapEventType.CheatSlapped);
    }
}
