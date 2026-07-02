using Unity.Netcode;
using UnityEngine;


public class BotBluffSlapReaction : ISlapReaction<BotSlapContext>
{
    public bool CanSlap(BotSlapContext context) =>
        context.BotBehavior != null && context.BluffController.IsBluffing;

    public void Slap(ulong slapperClientId, BotSlapContext context)
    {
        Debug.Log($"[Slap-Стратегия] Игрок {slapperClientId} купился на блеф!");

        if (context.Router.CasinoBank != null)
        {
            string tableType = context.GetCurrentTableType(); // ← Актуальный TableType
            int actualWithdrawn = context.Router.CasinoBank.TryWithdraw(
                1,
                slapperClientId,
                "SlapBluffPenalty",
                tableType);

            Debug.Log($"[Slap] Фактически списано: {actualWithdrawn}. Стол: {tableType}");
        }

        // TODO: Логика штрафа за ложное обвинение в блефе
        if (context.DispleasureController != null)
        {
            context.DispleasureController.AddInstantDispleasure(50f); // Мгновенный штраф недовольства
        }

        context.Router.TriggerEventClientRpc(BotSlapEventType.BluffSlapped);
    }
}