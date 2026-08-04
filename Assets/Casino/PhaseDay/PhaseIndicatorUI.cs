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

    [Header("Цвета состояний (опционально)")]
    [SerializeField] private Color lobbyColor = Color.gray;
    [SerializeField] private Color preparationColor = Color.yellow;
    [SerializeField] private Color gamePhaseColor = Color.green;
    [SerializeField] private Color dayEndingColor = Color.red;
    [SerializeField] private Color dayStatisticsColor = Color.cyan;
    [SerializeField] private Color loseGameColor = new Color(0.6f, 0f, 0f);
    [SerializeField] private Color winGameColor = new Color(1f, 0.84f, 0f);
    [SerializeField] private Color sessionEndedColor = Color.white;

    private void OnEnable()
    {
        var manager = GameSessionManager.Instance;
        if (manager != null)
        {
            manager.OnStateChanged += HandleStateChanged;
            manager.OnDayStarted += HandleDayStarted;

            // Обновляем начальное состояние
            UpdateDayText(manager.CurrentDay, manager.DayConfiguration.daysCount);
            HandleStateChanged(manager.CurrentState);
        }
    }

    private void OnDisable()
    {
        var manager = GameSessionManager.Instance;
        if (manager != null)
        {
            manager.OnStateChanged -= HandleStateChanged;
            manager.OnDayStarted -= HandleDayStarted;
        }
    }

    private void HandleDayStarted(int dayNumber)
    {
        var manager = GameSessionManager.Instance;
        if (manager != null)
        {
            UpdateDayText(dayNumber, manager.DayConfiguration.daysCount);
        }
    }

    private void HandleStateChanged(GameState state)
    {
        if (phaseText != null)
        {
            phaseText.text = GetStateName(state);
            phaseText.color = GetStateColor(state);
        }
    }

    private void UpdateDayText(int currentDay, int totalDays)
    {
        if (dayText != null)
        {
            dayText.text = $"День {currentDay}/{totalDays}";
        }
    }

    private string GetStateName(GameState state)
    {
        return state switch
        {
            GameState.Lobby => "Лобби",
            GameState.Preparing => "Подготовка",
            GameState.DayActive => "Игровой день",
            GameState.EndDay => "Завершение дня",
            GameState.DayStatistic => "Статистика дня",
            GameState.LoseGame => "Поражение",
            GameState.WinGame => "Победа",
            GameState.GameStatistic => "Итоги сессии",
            _ => "Неизвестно"
        };
    }

    private Color GetStateColor(GameState state)
    {
        return state switch
        {
            GameState.Lobby => lobbyColor,
            GameState.Preparing => preparationColor,
            GameState.DayActive => gamePhaseColor,
            GameState.EndDay => dayEndingColor,
            GameState.DayStatistic => dayStatisticsColor,
            GameState.LoseGame => loseGameColor,
            GameState.WinGame => winGameColor,
            GameState.GameStatistic => sessionEndedColor,
            _ => Color.white
        };
    }
}