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

        [Header("Настройки 'Дать пять'")]
        [Tooltip("Максимальный угол (в градусах) между взглядами игроков для взаимного шлепка.")]
        [SerializeField] private float highFiveMaxAngle = 45f;

        // События для визуального оформления (анимации, звуки, частицы)
        // Параметры: ID бьющего, нормализованная сила удара (0..1)
        public event Action<ulong, float, bool> OnSlapReceived;

        // Событие для атакующего: ID жертвы
        public event Action<ulong, bool> OnSlapAttacked;

        // Оставляем на случай, если другие скрипты хотят знать об откидывании
        public event Action<Vector3> OnKnockbackApplied;

        // Статус: игрок зажал кнопку и готов шлепнуть
        public bool IsSlapReady { get; private set; }

        // Вызывается из Interactable при нажатии кнопки
        public void SetSlapReady(bool ready)
        {
            IsSlapReady = ready;
        }

        /// <summary>
        /// Инициация шлепка от локального игрока (интерактора).
        /// </summary>
        public void InitiateSlap(ulong slapperClientId, float chargeTime, Vector3 slapperPosition, Vector3 slapperForward)
        {
            ReceiveSlapServerRpc(slapperClientId, chargeTime, slapperPosition, slapperForward);
        }

        // Измененный ReceiveSlapServerRpc
        [Rpc(SendTo.Server)]
        private void ReceiveSlapServerRpc(ulong slapperClientId, float chargeTime, Vector3 slapperPosition, Vector3 slapperForward)
        {
            // Проверка на "Дать пять"
            bool isHighFive = false;
            if (IsSlapReady)
            {
                // Проверяем, смотрят ли игроки друг на друга
                Vector3 victimForward = transform.forward;
                Vector3 toSlapper = (slapperPosition - transform.position).normalized;

                float angleVictimToSlapper = Vector3.Angle(victimForward, toSlapper);
                float angleSlapperToVictim = Vector3.Angle(slapperForward, -toSlapper);

                if (angleVictimToSlapper < highFiveMaxAngle && angleSlapperToVictim < highFiveMaxAngle)
                {
                    isHighFive = true;
                }
            }

            float forceNormalized = Mathf.Clamp01(chargeTime / maxChargeTime);

            // 1. Оповещаем всех о результате (обычный шлепок или "Дать пять")
            NotifySlapReceivedClientRpc(slapperClientId, forceNormalized, isHighFive);
            NotifySlapAttackedClientRpc(slapperClientId, OwnerClientId, isHighFive);

            // 2. Отбрасывание применяется ТОЛЬКО если это НЕ "Дать пять" и заряд достаточен
            if (!isHighFive && forceNormalized >= knockbackThreshold)
            {
                Vector3 knockbackDir = (transform.position - slapperPosition).normalized;
                knockbackDir.y = 0.6f;
                knockbackDir.Normalize();
                float appliedForce = Mathf.Lerp(minKnockbackForce, maxKnockbackForce, forceNormalized);
                Vector3 finalKnockbackVector = knockbackDir * appliedForce;

                ApplyKnockbackClientRpc(finalKnockbackVector);
            }

            // Сбрасываем готовность после удара
            IsSlapReady = false;
        }

        [Rpc(SendTo.Everyone)]
        private void NotifySlapReceivedClientRpc(ulong slapperClientId, float forceNormalized, bool isHighFive)
        {
            OnSlapReceived?.Invoke(slapperClientId, forceNormalized, isHighFive);
        }

        [Rpc(SendTo.Everyone)]
        private void NotifySlapAttackedClientRpc(ulong slapperClientId, ulong victimClientId, bool isHighFive)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(slapperClientId, out var client))
            {
                if (client.PlayerObject != null && client.PlayerObject.TryGetComponent<PlayerSlapReceiver>(out var slapperReceiver))
                {
                    slapperReceiver.TriggerSlapAttackedEvent(victimClientId, isHighFive);
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

        public void TriggerSlapAttackedEvent(ulong victimClientId, bool isHighFive)
        {
            OnSlapAttacked?.Invoke(victimClientId, isHighFive);
        }
    }
}