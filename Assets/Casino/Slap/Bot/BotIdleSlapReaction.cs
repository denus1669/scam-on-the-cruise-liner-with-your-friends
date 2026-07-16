using Unity.Netcode;
using UnityEngine;

public class BotIdleSlapReaction : ISlapReaction<BotSlapContext>
{
    public bool CanSlap(BotSlapContext context) => true;

    public void Slap(ulong slapperClientId, BotSlapContext context)
    {
        Debug.Log($"[Slap-Стратегия] Игрок {slapperClientId} ударил бота без причины!");

        if (context.Router.CasinoBank != null)
        {
            string tableType = context.GetCurrentTableType(); // ← Актуальный TableType
            int actualWithdrawn = context.Router.CasinoBank.TryWithdraw(
                1,
                slapperClientId,
                "SlapIdlePenalty",
                tableType);

            Debug.Log($"[Slap] Фактически списано: {actualWithdrawn}. Стол: {tableType}");
        }


        if (context.DispleasureController != null)
        {
            context.DispleasureController.AddInstantDispleasure(10f); // Мгновенный штраф недовольства
        }

        context.Router.TriggerEventClientRpc(BotSlapEventType.IdleSlapped);
    }
}