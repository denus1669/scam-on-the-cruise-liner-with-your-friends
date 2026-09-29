using UnityEngine;

namespace Assets.Casino.Slots
{
    /// <summary>
    /// Конфигурация для менеджера поломок игровых автоматов.
    /// </summary>
    [CreateAssetMenu(fileName = "SlotMachineBreakdownConfig", menuName = "Casino/Config/Slot Machine Breakdown")]
    public class SlotMachineBreakdownConfig : ScriptableObject
    {
        [Header("Настройки времени поломки (в секундах)")]
        [Min(1f)] public float minBreakTime = 10f;
        [Min(1f)] public float maxBreakTime = 30f;

        [Header("Настройки взрыва")]
        [Tooltip("Время в секундах, которое есть у игроков, чтобы починить автомат до взрыва")]
        [Min(1f)] public float timeBeforeExplode = 20f;

        [Tooltip("Количество раздражения, которое получают все боты при взрыве автомата")]
        [Min(0f)] public float explodeDispleasureAmount = 50f;

        [Header("Настройки синхронизации")]
        [Tooltip("Как часто обновлять время до взрыва на клиентах (в секундах). Меньше = плавнее, но дороже")]
        [Min(0.01f)] public float syncInterval = 0.1f;

        [Header("Настройки восстановления")]
        [Tooltip("Задержка в секундах перед восстановлением автомата после взрыва (чтобы VFX успели отобразиться)")]
        [Min(0f)] public float restoreDelay = 3f;
    }
}