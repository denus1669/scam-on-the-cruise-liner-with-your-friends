using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Отвечает ТОЛЬКО за зум камеры при включении режима внимания.
    /// Ничего не знает о сети, работает только с локальными событиями.
    /// </summary>
    [RequireComponent(typeof(PlayerAttentionController))]
    public class PlayerAttentionCamera : MonoBehaviour
    {
        [Header("Настройки камеры")]
        [SerializeField] private CoreCameraController cameraController;
        [SerializeField] private float defaultFOV = 60f;
        [SerializeField] private float attentionFOV = 25f;

        private PlayerAttentionController _attentionController;

        private void Awake()
        {
            _attentionController = GetComponent<PlayerAttentionController>();
        }

        private void OnEnable()
        {
            if (_attentionController == null)
            {
                return;
            }
            // Подписываемся только на локальное событие (зум нужен только нам)
            _attentionController.OnLocalAttentionChanged += ApplyCameraZoom;
        }

        private void OnDisable()
        {
            _attentionController.OnLocalAttentionChanged -= ApplyCameraZoom;
        }

        private void ApplyCameraZoom(bool isAttentionActive)
        {
            float targetFOV = isAttentionActive ? attentionFOV : defaultFOV;

            if (cameraController != null && cameraController.ActiveCameraMode?.CinemachineCamera != null)
            {
                cameraController.ActiveCameraMode.CinemachineCamera.Lens.FieldOfView = targetFOV;
            }
        }
    }
}