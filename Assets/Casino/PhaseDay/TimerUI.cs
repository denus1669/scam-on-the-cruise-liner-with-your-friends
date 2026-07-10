using TMPro;
using UnityEngine;

/// <summary>
/// UI-компонент для отображения таймера обратного отсчёта.
/// Активен только во время GamePhase.
/// </summary>
public class TimerUI : MonoBehaviour
{
    [Header("UI элементы")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject timerPanel; // Панель для скрытия/показа

    [Header("Настройки форматирования")]
    [SerializeField] private string formatString = "Осталось: {0:00}:{1:00}";

    private void OnEnable()
    {
        var manager = GameSessionManager.Instance;
        if (manager != null)
        {
            manager.OnPhaseChanged += HandlePhaseChanged;
            manager.OnTimerTick += HandleTimerTick;

            // Обновляем начальное состояние
            HandlePhaseChanged(manager.CurrentPhase);
            if (manager.CurrentPhase == GameSessionManager.SessionPhase.GamePhase)
            {
                HandleTimerTick(manager.TimeRemaining);
            }
        }
    }

    private void OnDisable()
    {
        var manager = GameSessionManager.Instance;
        if (manager != null)
        {
            manager.OnPhaseChanged -= HandlePhaseChanged;
            manager.OnTimerTick -= HandleTimerTick;
        }
    }

    private void HandlePhaseChanged(GameSessionManager.SessionPhase phase)
    {
        // Показываем таймер только во время GamePhase
        bool shouldShow = (phase == GameSessionManager.SessionPhase.GamePhase);

        if (timerPanel != null)
        {
            timerPanel.SetActive(shouldShow);
        }
    }

    private void HandleTimerTick(float remainingSeconds)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
            int seconds = Mathf.FloorToInt(remainingSeconds % 60f);

            timerText.text = string.Format(formatString, minutes, seconds);
        }
    }
}