using UnityEngine;

[CreateAssetMenu(fileName = "DayConfiguration", menuName = "Game/Day Configuration")]
public class DayConfiguration : ScriptableObject
{
    [Header("Общие настройки")]
    [Tooltip("Количество дней в сессии")]
    [Min(1)] public int daysCount = 5;

    [Tooltip("Длительность игровой фазы в секундах")]
    [Min(1f)] public float gamePhaseDuration = 180f;

    [Header("Настройки ботов")]
    [Tooltip("Максимальное количество ботов за день")]
    [Min(1)] public int botCount = 3;

    [Tooltip("Префабы ботов для спавна")]
    public GameObject[] botPrefabs;

    [Header("Настройки спавна")]
    [Tooltip("Сколько ботов спавнить сразу в начале дня")]
    [Min(0)] public int initialBotCount = 2;

    [Tooltip("Минимальная задержка между появлениями новых ботов (сек)")]
    [Min(0f)] public float spawnDelayMin = 55f;

    [Tooltip("Максимальная задержка между появлениями новых ботов (сек)")]
    [Min(0f)] public float spawnDelayMax = 155f;

    [Header("Условия победы/поражения")]
    [Tooltip("Минимальное количество фишек, которое должно быть в конце каждого дня. " +
             "Если фишек меньше - проигрыш. Индекс 0 = день 1, индекс 1 = день 2 и т.д.")]
    public int[] dailyChipThresholds = new int[] { 100, 150, 200, 300, 500 };

    private void OnValidate()
    {
        if (daysCount < 1) daysCount = 1;
        if (gamePhaseDuration < 1f) gamePhaseDuration = 1f;
        if (botCount < 1) botCount = 1;
        if (initialBotCount < 0) initialBotCount = 0;
        if (initialBotCount > botCount) initialBotCount = botCount;
        if (spawnDelayMin < 0f) spawnDelayMin = 0f;
        if (spawnDelayMax < spawnDelayMin) spawnDelayMax = spawnDelayMin;

        if (botPrefabs == null || botPrefabs.Length == 0)
        {
            Debug.LogWarning("[DayConfiguration] Не задан ни один префаб бота!");
        }
    }

    /// <summary>
    /// Возвращает порог фишек для заданного дня (1-based).
    /// Если массив короче или не задан, возвращает 0 (проигрыш только при отрицательном балансе).
    /// </summary>
    public int GetChipThresholdForDay(int day)
    {
        if (dailyChipThresholds == null || dailyChipThresholds.Length == 0)
            return 0;

        int index = day - 1; // day 1 -> index 0
        if (index < 0) index = 0;
        if (index >= dailyChipThresholds.Length)
            return dailyChipThresholds[dailyChipThresholds.Length - 1]; // последнее значение для дней вне массива

        return dailyChipThresholds[index];
    }
}