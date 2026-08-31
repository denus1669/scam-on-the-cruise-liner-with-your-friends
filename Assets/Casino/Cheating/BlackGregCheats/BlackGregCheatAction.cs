using UnityEngine;

/// <summary>
/// Специфичный базовый класс мухлежа для игры BlackGreg.
/// Разделяет логику на "фейковую" для бота и "честную" для игрока.
/// </summary>
public abstract class BlackGregCheatAction : CheatAction
{
    public override void ApplyCheatResult(IGameTable table, string who)
    {
        if (table is not BlackGregTable blackGregTable) return;

        ApplyCheat(blackGregTable, who);
    }

    /// <summary>
    /// Логика мухлежа для БОТА. 
    /// Здесь мы не считаем реальные карты, а просто подменяем руку на выигрышную заготовку.
    /// </summary>
    protected abstract void ApplyCheat(BlackGregTable table, string who);
}