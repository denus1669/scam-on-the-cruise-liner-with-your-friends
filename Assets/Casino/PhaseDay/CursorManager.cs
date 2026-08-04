using UnityEngine;

/// <summary>
/// Простой менеджер курсора.
/// При старте всегда освобождает курсор (для работы с SessionBrowser).
/// Включает курсор только для UI-фаз (DayStatistics, SessionEnded).
/// В остальных фазах — курсор в игровом режиме.
/// </summary>
public class CursorManager : MonoBehaviour
{
    [Header("Игровой режим (обычно FPS-стиль)")]
    [SerializeField] private bool gameplayCursorVisible = false;
    [SerializeField] private CursorLockMode gameplayLockMode = CursorLockMode.Locked;

    [Header("UI режим (для меню и статистики)")]
    [SerializeField] private bool uiCursorVisible = true;
    [SerializeField] private CursorLockMode uiLockMode = CursorLockMode.None;

    private GameSessionManager _manager;

    private void Start()
    {
        // Пытаемся получить менеджер
        _manager = GameSessionManager.Instance;
        if (_manager != null)
        {
            _manager.OnStateChanged += HandleStateChanged;
        }
    }

    private void OnDestroy()
    {
        if (_manager != null)
        {
            _manager.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.DayStatistic:
            case GameState.EndDay:
                Cursor.visible = uiCursorVisible;
                Cursor.lockState = uiLockMode;
                break;
            default:
                Cursor.visible = gameplayCursorVisible;
                Cursor.lockState = gameplayLockMode;
                break;
        }
    }
}