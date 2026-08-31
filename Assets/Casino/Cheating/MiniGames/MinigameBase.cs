using System;
using UnityEngine;


/// <summary>
/// Базовый класс для любой мини-игры в UI.
/// Вешается на корневой объект панели мини-игры внутри Canvas игрока.
/// </summary>
public abstract class MinigameBase : MonoBehaviour
{
    protected Action<bool> _onComplete;
    protected string _contextData; // Здесь будет имя чита или ID автомата
    protected bool _isPlaying = false;

    [Header("Базовые настройки")]
    [Tooltip("Корневой объект панели этой мини-игры")]
    [SerializeField] protected GameObject panelRoot;

    /// <summary>
    /// Инициализация и запуск.
    /// </summary>
    /// <param name="contextData">Строка с данными (имя чита, ID слота и т.д.)</param>
    /// <param name="onComplete">Коллбэк результата</param>
    public virtual void StartGame(string contextData, Action<bool> onComplete)
    {
        _contextData = contextData;
        _onComplete = onComplete;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        OnGameStarted();
    }

    /// <summary>
    /// Логика старта конкретной игры (сброс переменных, старт таймеров).
    /// Переопределяется в наследниках.
    /// </summary>
    protected virtual void OnGameStarted() { }

    /// <summary>
    /// Вызвать при победе.
    /// </summary>
    protected void FinishGame(bool isSuccess)
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        _onComplete?.Invoke(isSuccess);
        _onComplete = null; // Очищаем подписку
    }

    /// <summary>
    /// Принудительное закрытие без результата (например, игрок убежал).
    /// </summary>
    public virtual void ForceClose()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
        _onComplete = null;

        _isPlaying = false;
    }

    public virtual void WinGame()
    {
        Debug.Log("<color=green>Выигрыш</color>");
        FinishGame(true);
    }

    public virtual void LoseGame()
    {
        Debug.Log("<color=red>Проигрыш</color>");
        FinishGame(false);
    }
}
