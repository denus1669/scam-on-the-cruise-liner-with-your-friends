using Unity.Netcode;
using UnityEngine;

public class BotCheatSlapReaction : ISlapReaction<BotSlapContext>
{
    public bool CanSlap(BotSlapContext context) =>
           context.CheatController != null && context.CheatController.IsCheating;

    public void Slap(ulong slapperClientId, BotSlapContext context)
    {
        Debug.Log($"[Slap-Стратегия] Игрок {slapperClientId} ПОЙМАЛ бота на мухлеже!");

        if (context.Router.CasinoBank != null)
        {
            string tableType = context.GetCurrentTableType(); // ← Актуальный TableType
            bool actualWithdrawn = context.Router.CasinoBank.TryDeposit(
                1,
                slapperClientId,
                "SlapCheatingPenalty",
                tableType);

            Debug.Log($"[Slap] Фактически списано: {actualWithdrawn}. Стол: {tableType}");
        }

        context.CheatController.HandleCheatCaught(slapperClientId);
        context.Router.TriggerEventClientRpc(BotSlapEventType.CheatSlapped);
    }
}
