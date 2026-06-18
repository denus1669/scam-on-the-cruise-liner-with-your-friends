using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Компонент, управляющий уровнем раздражения/недовольства конкретного бота.
/// Отвечает за реакцию на пристальные взгляды игрока и завершение игры при "домогательстве".
/// </summary>
[RequireComponent(typeof(BotAgent))]
public class BotDispleasureController : NetworkBehaviour
{
    [Header("Настройки недовольства")]
    [Tooltip("Максимальный уровень недовольства, при котором бот прерывает игру.")]
    [SerializeField] private float maxDispleasure = 100f;
    [Tooltip("Время (в сек), в течение которого после подозрительного действия бот находится в сильной тревоге.")]
    [SerializeField] private float suspiciousTimeWindow = 3.5f;
    [Tooltip("Множитель роста раздражения, когда игрок пялится прямо во время/после анимации бота.")]
    [SerializeField] private float watchedMultiplier = 3.0f;
    [Tooltip("Базовое количество раздражения, которое накапливается за один тик.")]
    [SerializeField] private float baseDispleasureAmount = 0.5f;
    [Tooltip("Базовое количество раздражения, которое снижается за один тик.")]
    [SerializeField] private float baseDispleasureDecay = 20f;

    // Синхронизируемый уровень недовольства бота
    private readonly NetworkVariable<float> currentDispleasure = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private float lastSuspiciousActionTime = -100f; // Время старта последнего блефа или мухлежа
    private BotAgent botAgent;

    public float CurrentDispleasure => currentDispleasure.Value;

    public void Update()
    {
       // Debug.LogWarning($"[Displeasure] Текущий уровень недовольства бота {gameObject.name}: {currentDispleasure.Value}");
    }

    private void Awake()
    {
        botAgent = GetComponent<BotAgent>();
    }

    /// <summary>
    /// Фиксирует время начала подозрительного движения (мухлежа или блефа), чтобы увеличить чувствительность бота.
    /// </summary>
    public void NotifySuspiciousActionStarted()
    {
        if (!IsServer) return;
        lastSuspiciousActionTime = Time.time;
    }

    /// <summary>
    /// Накапливает раздражение бота на сервере, когда игрок использует на него Внимание.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void TickDispleasureServerRpc()
    {
        if (!IsServer) return;
        Debug.LogWarning($"[Displeasure] TickDispleasureServerRpc Бот {gameObject.name} !");

        // Если у бота сейчас нет активного стола или игра не запущена, раздражение не копится
        IGameTable currentTable = GetCurrentTable();
        if (currentTable == null || !currentTable.IsGameStarted) return;

        // Расчет множителя раздражения: если игрок прицелился сразу во время или после анимации
        float timeSinceAnimation = Time.time - lastSuspiciousActionTime;
        float multiplier = 1.0f;
        if (timeSinceAnimation < suspiciousTimeWindow)
        {
            multiplier = watchedMultiplier; // Бот начинает раздражаться в разы быстрее!
        }

        // Плавный рост раздражения
        currentDispleasure.Value = Mathf.Clamp(currentDispleasure.Value + (baseDispleasureAmount * multiplier), 0f, maxDispleasure);

        // Если бот вышел из себя из-за слишком наглого разглядывания
        if (currentDispleasure.Value >= maxDispleasure)
        {
            Debug.LogWarning($"[Displeasure] Бот {gameObject.name} взбешен постоянным разглядыванием!");

            // Наказываем игрока: останавливаем игру, победу отдаем боту (ulong.MaxValue)
            currentTable.ForceStopGame(ulong.MaxValue, isCheaterBot: true, reason: "Harassment");

            // Сбрасываем недовольство после завершения
            ResetDispleasureServerRpc();
        }
    }

    /// <summary>
    /// Сбрасывает уровень раздражения бота.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void ResetDispleasureServerRpc()
    {
        if (!IsServer) return;
            currentDispleasure.Value = Mathf.Clamp(currentDispleasure.Value - baseDispleasureDecay, 0f, maxDispleasure);


    }

    private IGameTable GetCurrentTable()
    {
        // Пытаемся получить стол напрямую через BotAgent, если на нем есть игровой бихейвиор
        if (botAgent != null && TryGetComponent<IBotGameBehavior>(out var behavior))
        {
            // Здесь предполагаем, что стол можно достать из текущего состояния агента. 
            // Для надежности можем найти BlackGregTable на сцене, за которым закреплен этот бот.
            var allTables = FindObjectsByType<GameTable>(FindObjectsSortMode.None);
            foreach (var table in allTables)
            {
                if (table.IsBotOccupied && table.botNetworkObjectRef.Value.TryGet(out NetworkObject botObj))
                {
                    if (botObj.gameObject == gameObject)
                    {
                        return table;
                    }
                }
            }
        }
        return null;
    }
}