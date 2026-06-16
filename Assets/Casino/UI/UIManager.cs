using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Центральный менеджер UI. Хранит список виджетов и предоставляет методы доступа к ним.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private List<UIWidget> widgets = new List<UIWidget>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        // Выводим список зарегистрированных виджетов
        Debug.Log($"[UIManager] Зарегистрировано виджетов: {widgets.Count}");
        foreach (var w in widgets)
            Debug.Log($"[UIManager] Виджет: {w.name} ({w.GetType().Name})");
    }

    /// <summary>
    /// Возвращает виджет указанного типа.
    /// </summary>
    public T GetWidget<T>() where T : UIWidget
    {
        foreach (var widget in widgets)
            if (widget is T t)
                return t;
        return null;
    }

    /// <summary>
    /// Показать виджет указанного типа.
    /// </summary>
    public void ShowWidget<T>() where T : UIWidget
    {
        var widget = GetWidget<T>();
        widget?.Show();
    }

    /// <summary>
    /// Скрыть виджет указанного типа.
    /// </summary>
    public void HideWidget<T>() where T : UIWidget
    {
        var widget = GetWidget<T>();
        widget?.Hide();
    }
}