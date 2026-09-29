using UnityEngine;

namespace Assets.Casino.Cheating
{
    /// <summary>
    /// Конфигурация подсветки читерских карт.
    /// </summary>
    [CreateAssetMenu(fileName = "CheatCardHighlightConfig", menuName = "Casino/Config/Cheat Card Highlight")]
    public class CheatCardHighlightConfig : ScriptableObject
    {
        [Header("Подсветка")]
        [Tooltip("Максимальная интенсивность свечения (значение для _IsCheated в шейдере)")]
        [Min(0f)] public float highlightIntensity = 1f;

        [Tooltip("Время плавного появления подсветки (секунды)")]
        [Min(0f)] public float fadeInDuration = 0.3f;

        [Tooltip("Время плавного затухания подсветки (секунды)")]
        [Min(0f)] public float fadeOutDuration = 0.5f;
    }
}