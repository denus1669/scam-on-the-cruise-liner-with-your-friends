using TMPro;
using UnityEngine;

/// <summary>
/// UI-компонент для отображения текущей фазы и номера дня.
/// Вешается на Canvas или UI-объект игрока.
/// </summary>
public class PhaseIndicatorUI : MonoBehaviour
{
    [Header("UI элементы")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI phaseText;

    [Header("Цвета фаз (опционально)")]
    [SerializeField] private Color preparationColor = Color.yellow;
    [SerializeField] private Color gamePhaseColor = Color.green;
    [SerializeField] private Color dayEndingColor = Color.red;
    [SerializeField] private Color dayStatisticsColor = Color.cyan;
    [SerializeField] private Color sessionEndedColor = Color.white;

    private void OnEnable()
    {
        var manager = GameSessionManager.Instance;
        if (manager != null)
        {
            manager.OnPhaseChanged += HandlePhaseChanged;
            manager.OnDayStarted += HandleDayStarted;

            // Обновляем начальное состояние
            UpdateDayText(manager.CurrentDay, manager.TotalDays);
            HandlePhaseChanged(manager.CurrentPhase);
        }
    }

    private void OnDisable()
    {
        var manager = GameSessionManager.Instance;
        if (manager != null)
        {
            manager.OnPhaseChanged -= HandlePhaseChanged;
            manager.OnDayStarted -= HandleDayStarted;
        }
    }

    private void HandleDayStarted(int dayNumber)
    {
        var manager = GameSessionManager.Instance;
        if (manager != null)
        {
            UpdateDayText(dayNumber, manager.TotalDays);
        }
    }

    private void HandlePhaseChanged(GameSessionManager.SessionPhase phase)
    {
        if (phaseText != null)
        {
            phaseText.text = GetPhaseName(phase);
            phaseText.color = GetPhaseColor(phase);
        }
    }

    private void UpdateDayText(int currentDay, int totalDays)
    {
        if (dayText != null)
        {
            dayText.text = $"День {currentDay}/{totalDays}";
        }
    }

    private string GetPhaseName(GameSessionManager.SessionPhase phase)
    {
        return phase switch
        {
            GameSessionManager.SessionPhase.Preparation => "Подготовка",
            GameSessionManager.SessionPhase.GamePhase => "Игровой день",
            GameSessionManager.SessionPhase.DayEnding => "Завершение дня",
            GameSessionManager.SessionPhase.DayStatistics => "Статистика дня",
            GameSessionManager.SessionPhase.SessionEnded => "Сессия завершена",
            _ => "Неизвестно"
        };
    }

    private Color GetPhaseColor(GameSessionManager.SessionPhase phase)
    {
        return phase switch
        {
            GameSessionManager.SessionPhase.Preparation => preparationColor,
            GameSessionManager.SessionPhase.GamePhase => gamePhaseColor,
            GameSessionManager.SessionPhase.DayEnding => dayEndingColor,
            GameSessionManager.SessionPhase.DayStatistics => dayStatisticsColor,
            GameSessionManager.SessionPhase.SessionEnded => sessionEndedColor,
            _ => Color.white
        };
    }
}