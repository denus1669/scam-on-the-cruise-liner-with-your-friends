namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Контракт для любого визуального индикатора мухлежа бота.
    /// Реализации: куб-меш, подсветка, свечение, частицы и т.д.
    /// </summary>
    public interface ICheatVisualIndicator
    {
        void ShowCheatIndicator();
        void HideCheatIndicator();
    }
}