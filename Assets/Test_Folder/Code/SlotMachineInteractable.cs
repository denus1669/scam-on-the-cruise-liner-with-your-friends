using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Интерактивный компонент автомата.
    /// Отвечает ТОЛЬКО за отображение UI (через интерфейс IInteractable) и передачу ввода игрока в ядро.
    /// </summary>
    public class SlotMachineInteractable : NetworkBehaviour, IInteractable
    {
        [Header("Слот-машина (Ядро)")]
        [SerializeField] private SlotMachine slotMachine;

        [Header("Настройки взаимодействия")]
        [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
        [SerializeField] private int priority = 10;

        // Кэш для оптимизации обновления текста подсказки (по аналогии с FinishGameInteractable)
        private string m_CachedPrompt;
        private bool m_LastIsBroken;

        public InteractionTriggerMode TriggerMode => triggerMode;
        public int Priority => priority;
        /// <summary>
        /// Динамическое время удержания
        /// </summary>
        public float HoldDuration => 2f;

        /// <summary>
        /// Динамический текст подсказки, зависящий от состояния ядра.
        /// </summary>
        public string InteractionPromptText
        {
            get
            {
                bool isBroken = slotMachine != null && slotMachine.IsBroken;

                // Обновляем кэш только если состояние изменилось
                if (isBroken != m_LastIsBroken || m_CachedPrompt == null)
                {
                    m_LastIsBroken = isBroken;
                    m_CachedPrompt = slotMachine == null ? "Автомат недоступен" :
                        isBroken ? $"Починить {slotMachine.machineName} (E)" : string.Empty;
                }

                return m_CachedPrompt;
            }
        }

        /// <summary>
        /// Определяет, может ли объект быть в фокусе.
        /// </summary>
        public bool CanInteract(GameObject interactor)
        {
            // Взаимодействовать можно только если автомат сломан
            if (slotMachine == null || !slotMachine.IsBroken)
                return false;

            return true;
        }

        /// <summary>
        /// Выполняется после успешного удержания/нажатия кнопки.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            if (!IsSpawned || slotMachine == null)
                return;

            // Получаем ID клиента, который нажал на кнопку
            if (interactor.TryGetComponent<NetworkObject>(out var netObj))
            {
                ulong clientId = netObj.OwnerClientId;
                // Дергаем ядро через ServerRpc
                slotMachine.TryFixMachineServerRpc(clientId);
            }
            else
            {
                Debug.LogWarning("[SlotMachineInteractable] Interactor не имеет NetworkObject!");
            }
        }
    }
}