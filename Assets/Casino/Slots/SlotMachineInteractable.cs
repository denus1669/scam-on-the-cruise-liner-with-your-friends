using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Интерактивный компонент автомата.
    /// Отвечает ТОЛЬКО за отображение UI (через интерфейс IInteractable) и передачу ввода игрока в ядро.
    /// Учитывает три состояния автомата: исправен, сломан, взорван.
    /// </summary>
    public class SlotMachineInteractable : NetworkBehaviour, IInteractable, IHoldReleaseInteractable
    {
        [Header("Слот-машина (Ядро)")]
        [SerializeField] private SlotMachine slotMachine;

        [Header("Настройки взаимодействия")]
        [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
        [SerializeField] private int priority = 10;

        [Header("Время удержания")]
        [Tooltip("Время удержания кнопки для починки сломанного автомата")]
        [SerializeField] private float fixHoldDuration = 2f;

        // Кэш для оптимизации обновления текста подсказки
        private string m_CachedPrompt;
        private bool m_LastIsBroken;
        private bool m_LastIsExploded;

        public InteractionTriggerMode TriggerMode => triggerMode;
        public int Priority => priority;

        /// <summary>
        /// Динамическое время удержания.
        /// Для взорванных автоматов - 0 (мгновенное нажатие, без удержания).
        /// Для сломанных - заданное время для починки.
        /// </summary>
        public float HoldDuration
        {
            get
            {
                if (slotMachine == null) return fixHoldDuration;

                // Взорванный автомат - мгновенное нажатие (информационное)
                if (slotMachine.IsExploded) return 0f;

                // Сломанный автомат - полное удержание для починки
                return fixHoldDuration;
            }
        }

        /// <summary>
        /// Динамический текст подсказки, зависящий от состояния ядра.
        /// </summary>
        public string InteractionPromptText
        {
            get
            {
                bool isBroken = slotMachine != null && slotMachine.IsBroken;
                bool isExploded = slotMachine != null && slotMachine.IsExploded;

                // Обновляем кэш только если состояние изменилось
                if (isBroken != m_LastIsBroken || isExploded != m_LastIsExploded || m_CachedPrompt == null)
                {
                    m_LastIsBroken = isBroken;
                    m_LastIsExploded = isExploded;

                    if (slotMachine == null)
                    {
                        m_CachedPrompt = "Автомат недоступен";
                    }
                    else if (isExploded)
                    {
                        m_CachedPrompt = $"{slotMachine.slotMachineName} уничтожен";
                    }
                    else if (isBroken)
                    {
                        m_CachedPrompt = $"Починить {slotMachine.slotMachineName} (E)";
                    }
                    else
                    {
                        m_CachedPrompt = string.Empty;
                    }
                }

                return m_CachedPrompt;
            }
        }

        public bool WaitForRelease => false;

        /// <summary>
        /// Определяет, может ли объект быть в фокусе и показывать текст подсказки.
        /// Возвращает true для сломанных И взорванных автоматов, чтобы текст показывался в обоих случаях.
        /// </summary>
        public bool CanInteract(GameObject interactor)
        {
            if (slotMachine == null)
                return false;

            // Показываем промпт для сломанных И взорванных автоматов
            // Реальное взаимодействие будет заблокировано в методе Interact()
            return slotMachine.IsBroken || slotMachine.IsExploded;
        }

        /// <summary>
        /// Выполняется после успешного удержания/нажатия кнопки.
        /// Для взорванных автоматов просто показывает сообщение и ничего не делает.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            if (!IsSpawned || slotMachine == null)
                return;

            // Взорванный автомат - только информационное сообщение, без реального взаимодействия
            if (slotMachine.IsExploded)
            {
                // Здесь можно добавить UI-уведомление игроку, например:
                // UIManager.ShowNotification("Этот автомат уничтожен и не может быть починен.");
                return;
            }

            // Двойная проверка перед отправкой RPC (на случай гонок состояния)
            if (!slotMachine.IsBroken)
            {
                return;
            }

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

        // IHoldReleaseInteractable - обработка отмены удержания
        public void OnHoldReleased(GameObject interactor, float holdTime)
        {
            if (!IsSpawned || slotMachine == null) return;

            // Если автомат был занят игроком для починки, освобождаем его
            if (slotMachine.IsOccupied && interactor.TryGetComponent<NetworkObject>(out var netObj))
            {
                ulong clientId = netObj.OwnerClientId;
                if (slotMachine.OccupiedByClientId == clientId)
                {
                    slotMachine.Leave(clientId);
                    Debug.Log($"[SlotMachineInteractable] Игрок {clientId} отменил починку, стол освобождён");
                }
            }
        }

        public void OnHoldStarted(GameObject interactor)
        {
            throw new System.NotImplementedException();
        }
    }
}