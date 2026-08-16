using UnityEngine;

/// <summary>
/// Специфичный базовый класс мухлежа для игры BlackGreg.
/// Разделяет логику на "фейковую" для бота и "честную" для игрока.
/// </summary>
public abstract class BlackGregBotCheatAction : CheatAction
{
    public override void ApplyCheatResult(IGameTable table)
    {
        if (table is not BlackGregTable blackGregTable) return;

        ApplyBotCheat(blackGregTable);
    }

    /// <summary>
    /// Логика мухлежа для БОТА. 
    /// Здесь мы не считаем реальные карты, а просто подменяем руку на выигрышную заготовку.
    /// </summary>
    protected abstract void ApplyBotCheat(BlackGregTable table);
}