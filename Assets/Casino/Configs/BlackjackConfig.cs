using UnityEngine;

/// <summary>
/// Конфигурация стола Блекджек для режима "Крупье vs Бот".
/// Управляет таймингами раунда, механикой подозрения и параметрами мухлежа.
/// </summary>
[CreateAssetMenu(fileName = "BlackjackConfig", menuName = "Casino/Configs/BlackjackConfig")]
public class BlackjackConfig : ScriptableObject
{
    [Header("Round Timing")]
    /// <summary>Длительность фазы раздачи карт до открытия окна мухлежа.</summary>
    public float dealingPhaseDuration = 3f;

    /// <summary>Время, доступное крупье для совершения мухлежа.</summary>
    public float cheatWindowDuration = 5f;

    /// <summary>Время показа результата и сброса стола.</summary>
    public float resolvePhaseDuration = 4f;

    [Header("Suspicion Mechanics")]
    /// <summary>Базовый рост подозрения при успешном мухлеже.</summary>
    public float suspicionGrowthOnSuccess = 12f;

    /// <summary>Рост подозрения при провале мини-игры мухлежа.</summary>
    public float suspicionGrowthOnFail = 25f;

    /// <summary>Скорость естественного затухания подозрения в секунду (когда крупье не мухлюет).</summary>
    public float suspicionDecayRate = 3f;

    /// <summary>Максимальное значение подозрения. При достижении раунд считается проваленным.</summary>
    public float maxSuspicion = 100f;

    [Header("Network & Interaction")]
    /// <summary>Дистанция для взаимодействия со столом.</summary>
    public float interactionDistance = 2.5f;
}

/// <summary>
/// Фазы раунда Блекджека в режиме Крупье.
/// </summary>
public enum BlackjackRoundPhase
{
    /// <summary>Стол ожидает крупье.</summary>
    Idle,

    /// <summary>Автоматическая раздача карт. Мухлеж недоступен.</summary>
    Dealing,

    /// <summary>Окно для мухлежа. Крупье может запустить мини-игру.</summary>
    CheatWindow,

    /// <summary>Подсчёт карт, применение результата, анимации.</summary>
    Resolving,

    /// <summary>Раунд завершён. Ожидание следующего цикла.</summary>
    RoundEnd
}

/// <summary>
/// Типы мухлежа, доступные крупье.
/// </summary>
public enum DealerCheatType
{
    None,
    MarkCards,      // Условная пометка карт
    SwapDeck,       // Быстрая замена колоды
    SecondSleeve    // Улучшение: второй рукав (открывается в магазине)
}