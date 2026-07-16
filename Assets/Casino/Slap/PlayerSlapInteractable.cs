using Unity.Netcode;
using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Компонент-адаптер для интеграции шлепков в систему CoreInteractor.
    /// Отвечает исключительно за логику фокуса, динамический текст подсказки и передачу ввода в ядро.
    /// </summary>
    [RequireComponent(typeof(PlayerSlapReceiver))]
    public class PlayerSlapInteractable : MonoBehaviour, IHoldReleaseInteractable
    {
        [Header("Компоненты")]
        [SerializeField] private PlayerSlapReceiver slapReceiver;

        [Header("Настройки удержания")]
        [Tooltip("Максимальное время зарядки удара (в секундах).")]
        [SerializeField] private float maxHoldDuration = 1.5f;
        [SerializeField] private int interactionPriority = 12;

        private string m_CachedPrompt;
        private bool m_LastCanInteract;

        public InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;
        public int Priority => interactionPriority;
        public float HoldDuration => maxHoldDuration;

        public string InteractionPromptText
        {
            get
            {
                // Кэшируем строку для оптимизации
                if (m_CachedPrompt == null)
                {
                    m_CachedPrompt = "[E] Дать леща (Удерживайте для силы)";
                }
                return m_CachedPrompt;
            }
        }

        private void Awake()
        {
            if (slapReceiver == null)
            {
                slapReceiver = GetComponent<PlayerSlapReceiver>();
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            if (slapReceiver == null) return false;

            // Нельзя ударить самого себя
            if (interactor.TryGetComponent<NetworkObject>(out var netObj))
            {
                return netObj.OwnerClientId != slapReceiver.OwnerClientId;
            }
            return true;
        }

        /// <summary>
        /// Вызывается, если игрок полностью завершил удержание (заряд на 100%).
        /// </summary>
        public void Interact(GameObject interactor)
        {
            TriggerSlapEvent(interactor, maxHoldDuration);
        }

        /// <summary>
        /// Вызывается, если игрок отпустил кнопку раньше времени (частичный заряд).
        /// </summary>
        public void OnHoldReleased(GameObject interactor, float chargeTime)
        {
            TriggerSlapEvent(interactor, chargeTime);
        }

        private void TriggerSlapEvent(GameObject interactor, float chargeTime)
        {
            if (slapReceiver == null) return;

            if (interactor.TryGetComponent<NetworkObject>(out var netObj))
            {
                ulong slapperId = netObj.OwnerClientId;
                Vector3 slapperPosition = interactor.transform.position;

                // Передаем управление в чистое ядро шлепка
                slapReceiver.InitiateSlap(slapperId, chargeTime, slapperPosition);
            }
        }
    }
}