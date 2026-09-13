using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Панель главного меню. Содержит кнопки и генерирует события.
/// </summary>
public class MainMenuPanel : MonoBehaviour
{
    public event Action OnContinuePressed;
    public event Action OnSettingsPressed;
    public event Action OnQuitPressed;

    [Header("Кнопки")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    private void OnEnable()
    {
        continueButton?.onClick.AddListener(HandleContinue);
        settingsButton?.onClick.AddListener(HandleSettings);
        quitButton?.onClick.AddListener(HandleQuit);
    }

    private void OnDisable()
    {
        continueButton?.onClick.RemoveListener(HandleContinue);
        settingsButton?.onClick.RemoveListener(HandleSettings);
        quitButton?.onClick.RemoveListener(HandleQuit);
    }

    private void HandleContinue()
    {
        Debug.Log("[MainMenu] Продолжить игру.");
        OnContinuePressed?.Invoke();
    }

    private void HandleSettings()
    {
        Debug.Log("[MainMenu] Открыть настройки.");
        OnSettingsPressed?.Invoke();
    }

    private void HandleQuit()
    {
        Debug.Log("[MainMenu] Запрос на выход из игры.");
        OnQuitPressed?.Invoke();
    }
}