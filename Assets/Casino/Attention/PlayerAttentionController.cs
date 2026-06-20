using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Отвечает ТОЛЬКО за состояние режима внимания (бинокль) и параметры камеры.
    /// Не знает ничего о картах или ботах (Соблюдение SRP).
    /// </summary>
    public class PlayerAttentionController : NetworkBehaviour
    {
        [Header("Режим внимания")]
        // Синхронизируем состояние бинокля для анимаций у других игроков
        public NetworkVariable<bool> IsAttention { get; private set; } = new NetworkVariable<bool>(false);

        [Header("Настройки камеры")]
        [SerializeField] private CoreCameraController cameraController;
        [SerializeField] private float defaultFOV = 60f;
        [SerializeField] private float attentionFOV = 25f;

        [Header("Ссылки")]
        [SerializeField] private CoreInputHandler inputHandler;
        [SerializeField] private AttentionRaycaster raycaster;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                IsAttention.OnValueChanged += OnAttentionStateChanged;
                OnAttentionStateChanged(false, IsAttention.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                IsAttention.OnValueChanged -= OnAttentionStateChanged;
            }
            base.OnNetworkDespawn();
        }

        /// <summary>
        /// Вызывается локально по кнопке инпута.
        /// </summary>
        public void ToggleAttentionLocal()
        {
            ToggleAttentionServerRpc(!IsAttention.Value);
        }

        [Rpc(SendTo.Server)]
        private void ToggleAttentionServerRpc(bool newState)
        {
            IsAttention.Value = newState;
        }

        private void OnAttentionStateChanged(bool previous, bool current)
        {
            // Здесь можно вызвать триггер аниматора: animator.SetBool("IsUsingBinoculars", current);

            if (IsOwner)
            {
                // Применяем зум камеры только для владельца
                float targetFOV = current ? attentionFOV : defaultFOV;
                ApplyCameraZoom(targetFOV);

                // Включаем/выключаем сканер (луч)
                if (raycaster != null)
                {
                    raycaster.SetRaycasterActive(current);
                }
            }
        }

        private void ApplyCameraZoom(float fov)
        {
            if (cameraController != null && cameraController.ActiveCameraMode?.CinemachineCamera != null)
            {
                cameraController.ActiveCameraMode.CinemachineCamera.Lens.FieldOfView = fov;
            }
        }
    }
}
