using UnityEngine;

/// <summary>
/// Базовый класс для любого UI-виджета. 
/// Все виджеты должны наследовать этот класс и реализовать Show/Hide.
/// </summary>
public abstract class UIWidget : MonoBehaviour
{
    public abstract void Show();
    public abstract void Hide();
}