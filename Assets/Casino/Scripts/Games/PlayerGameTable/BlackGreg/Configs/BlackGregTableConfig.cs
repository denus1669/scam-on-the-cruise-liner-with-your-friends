using UnityEngine;

namespace Assets.Casino.Games.BlackGreg
{
    /// <summary>
    /// Конфигурация стола BlackGreg: правила, тайминги, позиционирование карт и анимации.
    /// </summary>
    [CreateAssetMenu(fileName = "BlackGregTableConfig", menuName = "Casino/Config/BlackGreg Table")]
    public class BlackGregTableConfig : ScriptableObject
    {
        [Header("Позиционирование карт")]
        [Tooltip("Локальная позиция первой карты относительно родителя руки")]
        public Vector3 startPosition = new Vector3(0f, 0f, 0f);

        [Tooltip("Расстояние между картами в руке (веер)")]
        [Min(0f)] public float spreadDistance = 0.22f;

        [Tooltip("Начальный поворот карты (в локальных координатах родителя)")]
        public Vector3 startRotation = new Vector3(30f, 180f, 0f);

        [Header("Правила игры")]
        [Tooltip("Максимальное количество карт в руке (лимит добора)")]
        [Min(1)] public int cardLimit = 10;

        [Tooltip("Минимальное количество карт у каждой стороны, чтобы можно было завершить игру")]
        [Min(1)] public int minCardsToFinish = 2;

        [Header("Тайминги")]
        [Tooltip("Задержка после вскрытия карт перед автоматическим завершением игры (секунды)")]
        [Min(0f)] public float timeBeforeEvaluateResult = 2f;

        [Tooltip("Пауза для демонстрации результата (подсветка победителя / ничьей) перед сбросом карт")]
        [Min(0f)] public float highlightPauseDuration = 1.5f;

        [Header("Анимации")]
        [Tooltip("Конфиг анимаций карт (подсчёт, прыжок победителя, сброс)")]
        public CardAnimationConfig cardAnimationConfig;
    }
}