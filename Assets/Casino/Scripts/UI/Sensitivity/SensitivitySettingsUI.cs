using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Blocks.Gameplay.Core;

namespace Assets.Casino.UI
{
    /// <summary>
    /// Управляет настройкой чувствительности камеры через UI.
    /// Структура: SettingsSensitivity → ToggleSensitivity, SliderSensitivity, Text (TMP).
    /// </summary>
    public class SensitivitySettingsUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Toggle toggleSensitivity;
        [SerializeField] private Slider sliderSensitivity;
        [SerializeField] private TextMeshProUGUI valueText;

        [Header("Settings")]
        [SerializeField] private float minSensitivity = 0.01f;
        [SerializeField] private float maxSensitivity = 5f;
        [SerializeField] private float defaultSensitivity = 1f;

        private CoreCameraController _cameraController;
        private bool _isInitialized;

        private void Awake()
        {
            ConfigureSlider();
        }

        private void OnEnable()
        {
            if (!_isInitialized)
            {
                FindLocalCameraController();
                _isInitialized = _cameraController != null;
            }

            toggleSensitivity.onValueChanged.AddListener(OnToggleChanged);
            sliderSensitivity.onValueChanged.AddListener(OnSliderChanged);

            SyncFromCamera();
        }

        private void OnDisable()
        {
            toggleSensitivity.onValueChanged.RemoveListener(OnToggleChanged);
            sliderSensitivity.onValueChanged.RemoveListener(OnSliderChanged);
        }

        private void ConfigureSlider()
        {
            sliderSensitivity.minValue = minSensitivity;
            sliderSensitivity.maxValue = maxSensitivity;
            sliderSensitivity.value = defaultSensitivity;
            UpdateText(defaultSensitivity);
        }

        private void FindLocalCameraController()
        {
            var playerManagers = FindObjectsByType<CorePlayerManager>(FindObjectsSortMode.None);
            foreach (var pm in playerManagers)
            {
                if (pm.IsOwner)
                {
                    _cameraController = pm.CoreCamera;
                    break;
                }
            }
        }

        private void SyncFromCamera()
        {
            if (_cameraController == null) return;

            float current = _cameraController.CurrentLookSensitivity;
            sliderSensitivity.value = current;
            UpdateText(current);
            toggleSensitivity.isOn = true;
            sliderSensitivity.interactable = true;
        }

        private void OnToggleChanged(bool isOn)
        {
            sliderSensitivity.interactable = isOn;

            if (!isOn)
            {
                sliderSensitivity.value = defaultSensitivity;
                ApplySensitivity(defaultSensitivity);
            }
            else
            {
                ApplySensitivity(sliderSensitivity.value);
            }
        }

        private void OnSliderChanged(float value)
        {
            UpdateText(value);
            ApplySensitivity(value);
        }

        private void UpdateText(float value)
        {
            valueText.text = value.ToString("F2");
        }

        private void ApplySensitivity(float value)
        {
            if (_cameraController == null) return;
            _cameraController.SetLookSensitivity(value);
        }
    }
}