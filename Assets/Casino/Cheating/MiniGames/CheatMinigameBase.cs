using System;
using UnityEngine;

/// <summary>
/// Базовый класс для всех UI мини-игр мухлежа.
/// Размещается на корневом объекте (панели) мини-игры в Canvas.
/// </summary>
public abstract class CheatMinigameBase : MonoBehaviour
{
    protected Action<bool> onMinigameComplete;
    protected CheatAction currentCheatContext;

    [Header("Базовые UI элементы")]
    [Tooltip("Объект, который будет включаться/выключаться")]
    [SerializeField] protected GameObject minigamePanel;

    /// <summary>
    /// Инициализация и запуск мини-игры.
    /// </summary>
    /// <param name="cheatAction">Данные о выбранном мухлеже (можно использовать для сложности)</param>
    /// <param name="onComplete">Коллбэк, возвращающий true (успех) или false (провал)</param>
    public virtual void StartMinigame(CheatAction cheatAction, Action<bool> onComplete)
    {

        currentCheatContext = cheatAction;
        onMinigameComplete = onComplete;
        minigamePanel.SetActive(true);

        OnGameStarted();
    }

    /// <summary>Переопределяется наследниками для настройки старта (сброс таймеров и т.д.)</summary>
    protected virtual void OnGameStarted()
    {
    }

    /// <summary>Вызывается при успешном прохождении</summary>
    protected void WinMinigame()
    {
        minigamePanel.SetActive(false);
        onMinigameComplete?.Invoke(true);
    }

    /// <summary>Вызывается при ошибке/провале</summary>
    protected void LoseMinigame()
    {
        minigamePanel.SetActive(false);
        onMinigameComplete?.Invoke(false);
    }

    public void ForceClose()
    {
        minigamePanel.SetActive(false);
    }
}