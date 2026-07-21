using UnityEngine;

/// <summary>
/// Специфичный базовый класс мухлежа для игры BlackGreg.
/// Разделяет логику на "фейковую" для бота и "честную" для игрока.
/// </summary>
public abstract class BlackGregCheatAction : CheatAction
{
    public override void ApplyCheatResult(CheatContext context)
    {
        var table = context.GetTableAs<BlackGregTable>();
        if (table == null) return;

        if (context.IsBot)
        {
            ApplyBotCheat(table, context.Bot);
        }
        else
        {
            ApplyPlayerCheat(table, context.ClientId);
        } 
    }

    /// <summary>
    /// Логика мухлежа для БОТА. 
    /// Здесь мы не считаем реальные карты, а просто подменяем руку на выигрышную заготовку.
    /// </summary>
    protected abstract void ApplyBotCheat(BlackGregTable table, BotAgent bot);

    /// <summary>
    /// Логика мухлежа для ИГРОКА.
    /// Здесь в будущем будет сложная логика реальной подмены карт в руке игрока.
    /// </summary>
    protected abstract void ApplyPlayerCheat(BlackGregTable table, ulong playerId);
}