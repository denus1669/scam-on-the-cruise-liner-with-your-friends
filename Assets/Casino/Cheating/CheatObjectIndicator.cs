using UnityEngine;
using Blocks.Gameplay.Core;

/// <summary>
/// Простейшая реализация индикатора мухлежа: включает/выключает MeshRenderer на заданном объекте.
/// Висит на префабе бота.
/// </summary>
public class CheatObjectIndicator : NetworkObjectVisibilityIndicator, ICheatVisualIndicator
{
    // Вся логика MeshRenderer наследуется из NetworkObjectVisibilityIndicator.
    // Остаётся только реализовать контракт ICheatVisualIndicator.

    public void ShowCheatIndicator() => ShowIndicator();
    public void HideCheatIndicator() => HideIndicator();
}