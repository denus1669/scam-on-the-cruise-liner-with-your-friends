/// <summary>
/// Базовый интерфейс стратегии реакции на шлепок.
/// </summary>
public interface ISlapReaction<TContext>
{
    /// <summary>
    /// Проверяет, находится ли цель в состоянии, соответствующем этой реакции.
    /// </summary>
    bool CanSlap(TContext context);

    /// <summary>
    /// Выполняет саму реакцию (штрафы, VFX, остановку игры).
    /// </summary>
    void Slap(ulong slapperClientId, TContext context);
}