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
}