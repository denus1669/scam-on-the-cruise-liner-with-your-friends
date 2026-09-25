using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Slap
{
    [RequireComponent(typeof(PlayerSlapReceiver))]
    public class PlayerSlapInteractable : InteractableBase, IHoldReleaseInteractable
    {
        [Header("Компоненты")]
        [SerializeField] private PlayerSlapReceiver slapReceiver;

        [Header("Настройки удержания")]
        [Tooltip("Время до достижения 100% заряда. После этого можно держать бесконечно.")]
        [SerializeField] private float chargeTimeToMax = 2f;

        [Header("Настройки удержания")]
        [Tooltip("Максимальное время зарядки удара (в секундах).")]
        [SerializeField] private float maxHoldDuration = 999f;

        [SerializeField] private int interactionPriority = 12;

        [Tooltip("Если включено, удар не произойдет автоматически по таймеру. Нужно отпустить кнопку.")]
        [SerializeField] private bool waitForRelease = true;

        // Реализация свойства интерфейса
        public bool WaitForRelease => waitForRelease;

        // Флаг, чтобы InteractionAddon не терял цель, пока мы заряжаем удар
        private bool _isCharging = false;

        private string m_CachedPrompt;

        public override InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;
        public override int Priority => interactionPriority;
        public override float HoldDuration => maxHoldDuration;

        public override string InteractionPromptText
        {
            get
            {
                if (m_CachedPrompt == null)
                {
                    m_CachedPrompt = "[E] Шлепнуть (Удерживайте для силы)";
                }
                return m_CachedPrompt;
            }
        }

        private void Awake()
        {
            if (slapReceiver == null) slapReceiver = GetComponent<PlayerSlapReceiver>();
        }

        public override bool CanInteract(GameObject interactor)
        {
            if (slapReceiver == null) return false;

            // Если идет зарядка, разрешаем взаимодействие всегда, чтобы не сбивать фокус при повороте камеры
            if (_isCharging) return true;

            // Нельзя ударить самого себя
            if (interactor.TryGetComponent<NetworkObject>(out var netObj))
            {
                return netObj.OwnerClientId != slapReceiver.OwnerClientId;
            }
            return true;
        }

        public override void Interact(GameObject interactor)
        {
            TriggerSlapEvent(interactor, maxHoldDuration);
        }

        public void OnHoldReleased(GameObject interactor, float holdTimer)
        {
            // Сбрасываем флаг зарядки
            _isCharging = false;

            if (slapReceiver == null) return;
            if (!interactor.TryGetComponent<NetworkObject>(out var netObj)) return;

            ulong slapperId = netObj.OwnerClientId;
            Vector3 slapperPosition = interactor.transform.position;
            Vector3 slapperForward = interactor.transform.forward; // Направление взгляда в момент отпускания

            // Передаем РЕАЛЬНОЕ время удержания. 
            // Clamp до chargeTimeToMax произойдет внутри Receiver.
            slapReceiver.InitiateSlap(slapperId, holdTimer, slapperPosition, slapperForward);
        }

        /// <summary>
        /// Вызывается из InteractionAddon при начале удержания.
        /// Требует добавления void OnHoldStarted(GameObject) в интерфейс IHoldReleaseInteractable.
        /// </summary>
        public void OnHoldStarted(GameObject interactor)
        {
            _isCharging = true;
        }

        private void TriggerSlapEvent(GameObject interactor, float chargeTime)
        {
            if (slapReceiver == null) return;
            if (interactor.TryGetComponent<NetworkObject>(out var netObj))
            {
                ulong slapperId = netObj.OwnerClientId;
                Vector3 slapperPosition = interactor.transform.position;
                Vector3 slapperForward = interactor.transform.forward; // Направление взгляда в момент удара

                slapReceiver.InitiateSlap(slapperId, chargeTime, slapperPosition, slapperForward);
            }
        }

        public override bool HasAnyAvailableInteractor()
        {
            throw new System.NotImplementedException();
        }
    }
}