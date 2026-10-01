using UnityEngine;

namespace Assets.Casino.Bot
{
    /// <summary>
    /// Конфигурация зрения бота.
    /// </summary>
    [CreateAssetMenu(fileName = "BotVisionConfig", menuName = "Casino/Config/Bot Vision")]
    public class BotVisionConfig : ScriptableObject
    {
        [Header("Конус зрения")]
        [Tooltip("Угол обзора бота в градусах (полный угол конуса)")]
        [Range(10f, 360f)] public float viewAngle = 180f;

        [Tooltip("Максимальная дальность зрения бота")]
        [Min(0.1f)] public float viewDistance = 10f;

        [Header("Отладка")]
        [Tooltip("Цвет гизмо конуса зрения")]
        public Color debugColor = Color.yellow;
    }
}