using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsInputPanel : MonoBehaviour
{
    public event Action OnBackPressed;

    [Header("Чувствительность мыши")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private float minSensitivity = 0.1f;
    [SerializeField] private float maxSensitivity = 5f;

    [Header("Кнопки")]
    [SerializeField] private Button backButton;

    // Событие для применения чувствительности (подпишется камера или менеджер)
    public event Action<float> OnSensitivityChanged;

    private void OnEnable()
    {
        backButton?.onClick.AddListener(() => OnBackPressed?.Invoke());
        sensitivitySlider?.onValueChanged.AddListener(HandleSensitivityChanged);
    }

    private void OnDisable()
    {
        backButton?.onClick.RemoveAllListeners();
        sensitivitySlider?.onValueChanged.RemoveListener(HandleSensitivityChanged);
    }

    private void Start()
    {
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = minSensitivity;
            sensitivitySlider.maxValue = maxSensitivity;
        }
    }

    private void HandleSensitivityChanged(float value)
    {
        Debug.Log($"[SettingsInput] Чувствительность: {value}");
        OnSensitivityChanged?.Invoke(value);
    }
}