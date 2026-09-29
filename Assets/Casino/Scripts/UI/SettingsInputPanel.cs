using System;
using UnityEngine;
using UnityEngine.UI;
using Blocks.Gameplay.Core;

namespace Assets.Casino.UI
{
    public class SettingsInputPanel : MonoBehaviour
    {
        public event Action OnBackPressed;

        [Header("Чувствительность мыши")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private float minSensitivity = 0.01f;
        [SerializeField] private float maxSensitivity = 5f;

        [Header("Кнопки")]
        [SerializeField] private Button backButton;

        // Событие для применения чувствительности (подпишется камера или менеджер)
        public event Action<float> OnSensitivityChanged;

        private CoreCameraController _cameraController;

        private void Awake()
        {
            FindLocalCameraController();
        }

        private void OnEnable()
        {
            backButton?.onClick.AddListener(() => OnBackPressed?.Invoke());
            sensitivitySlider?.onValueChanged.AddListener(HandleSensitivityChanged);

            // Синхронизируем слайдер с текущим значением при открытии
            SyncSliderFromCamera();
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

        private void SyncSliderFromCamera()
        {
            if (_cameraController == null || sensitivitySlider == null) return;

            float currentSensitivity = _cameraController.CurrentLookSensitivity;
            sensitivitySlider.value = currentSensitivity;
        }

        private void HandleSensitivityChanged(float value)
        {
            Debug.Log($"[SettingsInput] Чувствительность: {value:F2}");
            OnSensitivityChanged?.Invoke(value);
            ApplySensitivity(value);
        }

        private void ApplySensitivity(float value)
        {
            if (_cameraController == null) return;
            _cameraController.SetLookSensitivity(value);
        }
    }
}