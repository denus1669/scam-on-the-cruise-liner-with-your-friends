using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Интерактивный компонент инспектора.
    /// Позволяет игроку зажать E, чтобы остановить инспектора на N секунд.
    /// После использования уходит на перезарядку.
    /// </summary>
    public class InspectorInteractable : NetworkBehaviour, IInteractable, IHoldReleaseInteractable
    {
        [Header("Инспектор")]
        [SerializeField] private InspectorAgent inspector;

        [Header("Настройки взаимодействия")]
        [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
        [SerializeField] private int priority = 10;

        [Header("Время удержания")]
        [Tooltip("Время удержания кнопки E для остановки инспектора")]
        [SerializeField] private float distractHoldDuration = 2f;

        private string _cachedPrompt;
        private bool _lastIsStopped;
        private float _lastCooldownRemaining = -1f;

        public InteractionTriggerMode TriggerMode => triggerMode;
        public int Priority => priority;

        /// <summary>
        /// Время удержания для виджета загрузки.
        /// </summary>
        public float HoldDuration => distractHoldDuration;

        /// <summary>
        /// Динамический текст подсказки.
        /// </summary>
        public string InteractionPromptText
        {
            get
            {
                bool isStopped = inspector != null && inspector.IsStoppedByPlayer;
                float cooldown = inspector != null ? inspector.GetInteractionCooldownRemaining() : 0f;

                // Обновляем кэш только если состояние изменилось
                if (isStopped != _lastIsStopped ||
                    Mathf.Abs(cooldown - _lastCooldownRemaining) > 0.1f ||
                    _cachedPrompt == null)
                {
                    _lastIsStopped = isStopped;
                    _lastCooldownRemaining = cooldown;

                    if (inspector == null)
                    {
                        _cachedPrompt = "Инспектор недоступен";
                    }
                    else if (isStopped)
                    {
                        _cachedPrompt = "Инспектор остановлен";
                    }
                    else if (cooldown > 0f)
                    {
                        _cachedPrompt = $"Перезарядка: {cooldown:F1}с";
                    }
                    else
                    {
                        _cachedPrompt = "Отвлечь инспектора (E)";
                    }
                }

                return _cachedPrompt;
            }
        }

        /// <summary>
        /// Может ли игрок взаимодействовать с инспектором.
        /// </summary>
        public bool CanInteract(GameObject interactor)
        {
            if (inspector == null)
                return false;

            // Нельзя взаимодействовать, если инспектор уже остановлен или на кулдауне
            return !inspector.IsStoppedByPlayer && inspector.GetInteractionCooldownRemaining() <= 0f;
        }

        /// <summary>
        /// Вызывается после успешного удержания кнопки.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            if (!IsSpawned || inspector == null)
                return;

            if (!inspector.IsSpawned)
                return;

            // Получаем ID клиента, который нажал на кнопку
            if (interactor.TryGetComponent<NetworkObject>(out var netObj))
            {
                ulong clientId = netObj.OwnerClientId;
                // Вызываем инспектора через ServerRpc
                inspector.RequestStopByPlayerInteractionServerRpc(clientId);
            }
            else
            {
                Debug.LogWarning("[InspectorInteractable] Interactor не имеет NetworkObject!");
            }
        }

        /// <summary>
        /// Обработка отмены удержания (игрок отпустил E раньше времени).
        /// </summary>
        public void OnHoldReleased(GameObject interactor, float holdTime)
        {
            // Для инспектора отмена удержания не требует дополнительных действий.
            // Если нужно прерывать анимацию или звук — добавьте сюда.
            if (!IsSpawned || inspector == null) return;

            Debug.Log($"[InspectorInteractable] Игрок отменил взаимодействие с инспектором (удержано {holdTime:F2}с)");
        }
    }
}