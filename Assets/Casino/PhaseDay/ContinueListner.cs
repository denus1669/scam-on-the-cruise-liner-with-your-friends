using Blocks.Gameplay.Core;
using UnityEngine;

/// <summary>
/// Слушатель действия "Продолжить".
/// Поддерживает два режима:
/// - Мгновенный (через кнопку UI)
/// - С удержанием (через клавишу F, 2 секунды)
/// </summary>
public class ContinueListner : MonoBehaviour
{
    [Header("События ввода")]
    [Tooltip("GameEvent при НАЖАТИИ клавиши продолжения")]
    [SerializeField] private GameEvent onContinuePressed;

    [Tooltip("GameEvent при ОТПУСКАНИИ клавиши продолжения")]
    [SerializeField] private GameEvent onContinueReleased;

    [Header("Настройки удержания")]
    [SerializeField] private float holdDuration = 2f;

    // Состояние удержания
    private bool _isHolding;
    private float _holdTimer;

    private void OnEnable()
    {
        if (onContinuePressed != null)
            onContinuePressed.RegisterListener(OnContinueStarted);

        if (onContinueReleased != null)
            onContinueReleased.RegisterListener(OnContinueCancelled);
    }

    private void OnDisable()
    {
        if (onContinuePressed != null)
            onContinuePressed.UnregisterListener(OnContinueStarted);

        if (onContinueReleased != null)
            onContinueReleased.UnregisterListener(OnContinueCancelled);

        CancelHold();
    }

    private void Update()
    {
        if (!_isHolding) return;

        _holdTimer += Time.deltaTime;

        // Удержание завершено
        if (_holdTimer >= holdDuration)
        {
            _isHolding = false;
            _holdTimer = 0f;
            Debug.Log($"[ContinueListner] ✅ Удержание завершено ({holdDuration} сек)");
            OnContinueActivated();
        }
    }

    /// <summary>
    /// Вызывается при НАЖАТИИ клавиши (через GameEvent)
    /// или при клике на кнопку UI (напрямую).
    /// </summary>
    public void OnContinueStarted()
    {
        var manager = GameSessionManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[ContinueListner] GameSessionManager.Instance не найден");
            return;
        }

        if (manager.CurrentState != GameState.DayStatistic &&
            manager.CurrentState != GameState.EndDay)
        {
            Debug.Log($"[ContinueListner] Не в нужной фазе (текущая: {manager.CurrentState})");
            return;
        }

        if (holdDuration <= 0f)
        {
            // Мгновенное срабатывание (для кнопки UI)
            Debug.Log("[ContinueListner] Мгновенное продолжение");
            OnContinueActivated();
        }
        else
        {
            // Начинаем удержание (для клавиши)
            _isHolding = true;
            _holdTimer = 0f;
            Debug.Log($"[ContinueListner] Начало удержания, нужно {holdDuration} сек");
        }
    }

    /// <summary>
    /// Вызывается при ОТПУСКАНИИ клавиши (через GameEvent)
    /// </summary>
    public void OnContinueCancelled()
    {
        if (_isHolding)
        {
            Debug.Log($"[ContinueListner] Удержание отменено через {_holdTimer:F2} сек");
            CancelHold();
        }
    }

    private void CancelHold()
    {
        _isHolding = false;
        _holdTimer = 0f;
    }

    /// <summary>
    /// Финальное действие — отправка RPC на сервер
    /// </summary>
    private void OnContinueActivated()
    {
        var manager = GameSessionManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[ContinueListner] GameSessionManager.Instance не найден");
            return;
        }

        Debug.Log("[ContinueListner] Игрок подтвердил продолжение");
        manager.RequestContinueServerRpc();
    }
}