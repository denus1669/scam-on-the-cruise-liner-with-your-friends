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

        [SerializeField, Min(1f)] private float _multiplicatorMin = 1f;
        [SerializeField, Min(1f)] private float _multiplicatorMax = 5f;
        [SerializeField] private float _weakSlap = 1;
        [SerializeField] private float _strongSlap = 150;

        [Header("Спец-удары (не зависят от chargeTime)")]
        [SerializeField, Range(0f, 1f)] private float _weakSlapChance = 0.05f;   // 5%
        [SerializeField, Range(0f, 1f)] private float _strongSlapChance = 0.03f; // 3%
        [SerializeField, Range(0f, 1f)] private float _multiplicatorChance = 0.3f; // 30% шанс

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
                Vector3 victimForward = transform.forward;
                Vector3 toSlapper = (slapperPosition - transform.position).normalized;

                float angleVictimToSlapper = Vector3.Angle(victimForward, toSlapper);
                float angleSlapperToVictim = Vector3.Angle(slapperForward, -toSlapper);

                if (angleVictimToSlapper < highFiveMaxAngle && angleSlapperToVictim < highFiveMaxAngle)
                    isHighFive = true;
            }

            // === 1. ОПРЕДЕЛЕНИЕ ТИПА УДАРА И СИЛЫ ===
            float forceNormalized = Mathf.Clamp01(chargeTime / maxChargeTime);
            bool isSpecialSlap = false;
            float appliedForce;

            float roll = UnityEngine.Random.value;
            if (roll < _weakSlapChance)
            {
                appliedForce = _weakSlap;
                forceNormalized = Mathf.Clamp01(_weakSlap / maxKnockbackForce); // Для анимации/звука
                isSpecialSlap = true;
                Debug.Log($"[Slap] WEAK SLAP! force={appliedForce:F1}");
            }
            else if (roll < _weakSlapChance + _strongSlapChance)
            {
                appliedForce = _strongSlap;
                forceNormalized = 1f; // Для анимации/звука
                isSpecialSlap = true;
                Debug.Log($"[Slap] STRONG SLAP! force={appliedForce:F1}");
            }
            else
            {
                // Обычный удар: сила зависит от зарядки
                appliedForce = Mathf.Lerp(minKnockbackForce, maxKnockbackForce, forceNormalized);
            }

            // === 2. СЛУЧАЙНЫЙ МНОЖИТЕЛЬ (после определения типа) ===
            if (UnityEngine.Random.value < _multiplicatorChance)
            {
                float randomMult = UnityEngine.Random.Range(_multiplicatorMin, _multiplicatorMax);
                appliedForce *= randomMult;
                Debug.Log($"[Slap] MULTIPLIER x{randomMult:F2} -> force={appliedForce:F1}");
            }

            // Оповещаем клиентов (для анимаций/звуков)
            NotifySlapReceivedClientRpc(slapperClientId, forceNormalized, isHighFive);
            NotifySlapAttackedClientRpc(slapperClientId, OwnerClientId, isHighFive);

            // === 3. ОТБРАСЫВАНИЕ ===
            // Спец-удары всегда отбрасывают; обычные — только если заряд >= порога
            bool shouldKnockback = !isHighFive && (isSpecialSlap || forceNormalized >= knockbackThreshold);

            if (shouldKnockback)
            {
                Vector3 knockbackDir = (transform.position - slapperPosition).normalized;
                knockbackDir.y = 0.6f;
                knockbackDir.Normalize();

                Vector3 finalKnockbackVector = knockbackDir * appliedForce;
                ApplyKnockbackClientRpc(finalKnockbackVector);
            }

            IsSlapReady = false;
        }

        private void GiveFive()
        {

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
        }

        public void TriggerSlapAttackedEvent(ulong victimClientId, bool isHighFive)
        {
            OnSlapAttacked?.Invoke(victimClientId, isHighFive);
        }
    }
}