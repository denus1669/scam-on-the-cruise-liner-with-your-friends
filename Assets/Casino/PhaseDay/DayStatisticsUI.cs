using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI-компонент для экрана статистики дня.
/// Показывается в фазе DayStatistics, содержит кнопку "Продолжить".
/// </summary>
public class DayStatisticsUI : MonoBehaviour
{
    [Header("UI элементы")]
    [SerializeField] private GameObject DayStatisticsPanel;
    [SerializeField] private TextMeshProUGUI dayTitleText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI continueButtonText;

    private void OnEnable()
    {
        var manager = GameSessionManager.Instance;
        if (manager != null)
        {
            manager.OnPhaseChanged += HandlePhaseChanged;
            manager.OnDayEnded += HandleDayEnded;

            // Обновляем начальное состояние
            HandlePhaseChanged(manager.CurrentPhase);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    private void OnDisable()
    {
        var manager = GameSessionManager.Instance;
        if (manager != null)
        {
            manager.OnPhaseChanged -= HandlePhaseChanged;
            manager.OnDayEnded -= HandleDayEnded;
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
        }
    }

    private void HandlePhaseChanged(GameSessionManager.SessionPhase phase)
    {
        bool shouldShow = (phase == GameSessionManager.SessionPhase.DayStatistics);

        if (DayStatisticsPanel != null)
        {
            DayStatisticsPanel.SetActive(shouldShow);
        }
    }

    private void HandleDayEnded(int finishedDay)
    {
        // Обновляем заголовок
        if (dayTitleText != null)
        {
            dayTitleText.text = $"День {finishedDay} завершён";
        }

        // TODO: Здесь будет получение статистики из StatisticsCollector
        // Пока показываем заглушку
        if (statsText != null)
        {
            statsText.text = "Статистика дня:\n" +
                             "Игр сыграно: 0\n" +
                             "Побед: 0\n" +
                             "Поражений: 0\n" +
                             "Баланс: +0 фишек";
        }

        // Обновляем текст кнопки в зависимости от того, последний ли это день
        var manager = GameSessionManager.Instance;
        if (manager != null && continueButtonText != null)
        {
            bool isLastDay = (finishedDay >= manager.TotalDays);
            continueButtonText.text = isLastDay ? "Завершить сессию" : "Следующий день";
        }
    }

    private void OnContinueClicked()
    {
        var manager = GameSessionManager.Instance;
        if (manager != null)
        {
            manager.RequestContinueServerRpc();
        }
    }
}