using System;
using Unity.Netcode;
using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Ядро механики шлепка. Управляет расчетом силы, сетевой синхронизацией,
    /// применением физического импульса отбрасывания и рассылкой событий для анимации и звука.
    /// </summary>
    public class PlayerSlapReceiver : NetworkBehaviour
    {
        [Header("Настройки отбрасывания")]
        [Tooltip("Максимальное время зарядки. Должно соответствовать HoldDuration в Interactable.")]
        [SerializeField] private float maxChargeTime = 1.5f;

        [Tooltip("Минимальная сила отбрасывания (при быстром клике).")]
        [SerializeField] private float minKnockbackForce = 0f;

        [Tooltip("Максимальная сила отбрасывания при 100% зажатии.")]
        [SerializeField] private float maxKnockbackForce = 18f;

        [Tooltip("Минимальная сила заряда (0..1) для активации отлета. Все что ниже — просто звук шлепка.")]
        [Range(0f, 1f)]
        [SerializeField] private float knockbackThreshold = 0.25f;

        // События для визуального оформления (анимации, звуки, частицы)
        // Параметры: ID бьющего, нормализованная сила удара (0..1)
        public event Action<ulong, float> OnSlapReceived;

        // Событие для атакующего: ID жертвы
        public event Action<ulong> OnSlapAttacked;

        // Оставляем на случай, если другие скрипты хотят знать об откидывании
        public event Action<Vector3> OnKnockbackApplied;

        /// <summary>
        /// Инициация шлепка от локального игрока (интерактора).
        /// </summary>
        public void InitiateSlap(ulong slapperClientId, float chargeTime, Vector3 slapperPosition)
        {
            ReceiveSlapServerRpc(slapperClientId, chargeTime, slapperPosition);
        }

        [Rpc(SendTo.Server)]
        private void ReceiveSlapServerRpc(ulong slapperClientId, float chargeTime, Vector3 slapperPosition)
        {
            float forceNormalized = Mathf.Clamp01(chargeTime / maxChargeTime);

            // 1. Оповещаем всех клиентов, что жертва получила шлепок
            NotifySlapReceivedClientRpc(slapperClientId, forceNormalized);

            // 2. Оповещаем всех клиентов, что атакующий совершил удар
            NotifySlapAttackedClientRpc(slapperClientId, OwnerClientId);

            // 3. Если удар достаточно заряжен, рассчитываем отбрасывание для пострадавшего
            if (forceNormalized >= knockbackThreshold)
            {
                // Направление от бьющего к жертве
                Vector3 knockbackDir = (transform.position - slapperPosition).normalized;

                // Добавляем подброс вверх для красивого отрыва от земли (настраивается)
                knockbackDir.y = 0.6f;
                knockbackDir.Normalize();

                float appliedForce = Mathf.Lerp(minKnockbackForce, maxKnockbackForce, forceNormalized);
                Vector3 finalKnockbackVector = knockbackDir * appliedForce;

                // Передаем импульс только владельцу персонажа-жертвы
                ApplyKnockbackClientRpc(finalKnockbackVector);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void NotifySlapReceivedClientRpc(ulong slapperClientId, float forceNormalized)
        {
            OnSlapReceived?.Invoke(slapperClientId, forceNormalized);
        }

        [Rpc(SendTo.Everyone)]
        private void NotifySlapAttackedClientRpc(ulong slapperClientId, ulong victimClientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(slapperClientId, out var client))
            {
                if (client.PlayerObject != null && client.PlayerObject.TryGetComponent<PlayerSlapReceiver>(out var slapperReceiver))
                {
                    slapperReceiver.TriggerSlapAttackedEvent(victimClientId);
                }
            }
        }

        [Rpc(SendTo.Owner)]
        private void ApplyKnockbackClientRpc(Vector3 knockbackVector)
        {
            OnKnockbackApplied?.Invoke(knockbackVector);

            // --- ИНТЕГРАЦИЯ С CORE MOVEMENT ---
            if (TryGetComponent<CoreMovement>(out var coreMovement))
            {
                // 1. Применяем горизонтальную силу отлета (X, Z)
                Vector3 horizontalForce = new Vector3(knockbackVector.x, 0, knockbackVector.z);
                coreMovement.ApplyExternalForce(horizontalForce, ForceMode.Impulse);

                // 2. Применяем вертикальную силу (подбрасываем игрока в воздух)
                if (knockbackVector.y > 0)
                {
                    // Добавляем скорость по Y напрямую (преодолевая гравитацию)
                    coreMovement.SetVerticalVelocity(knockbackVector.y);
                }
            }
            // Фолбэк для классических объектов с Rigidbody (если вдруг шлепки будут применяться к физическим пропсам)
            else if (TryGetComponent<Rigidbody>(out var rb) && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.AddForce(knockbackVector, ForceMode.Impulse);
            }
        }

        public void TriggerSlapAttackedEvent(ulong victimClientId)
        {
            OnSlapAttacked?.Invoke(victimClientId);
        }
    }
}