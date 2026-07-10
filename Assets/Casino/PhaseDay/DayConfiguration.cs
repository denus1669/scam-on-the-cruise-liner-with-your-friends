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
    [Tooltip("Количество ботов за день")]
    [Min(1)] public int botCount = 3;

    [Tooltip("Префабы ботов для спавна")]
    public GameObject[] botPrefabs;

    private void OnValidate()
    {
        if (daysCount < 1) daysCount = 1;
        if (gamePhaseDuration < 1f) gamePhaseDuration = 1f;
        if (botCount < 1) botCount = 1;

        if (botPrefabs == null || botPrefabs.Length == 0)
        {
            Debug.LogWarning("[DayConfiguration] Не задан ни один префаб бота!");
        }
    }
}