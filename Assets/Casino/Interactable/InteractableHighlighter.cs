using Assets.Casino.Cheating;
using Assets.Casino.QuickOutline.Scripts;
using Unity.Netcode;
using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Базовый класс подсветки интерактивных объектов через Outline (обводку).
    ///
    /// Отвечает за:
    /// - локальную подсветку фокуса;
    /// - сетевую подсветку доступности;
    /// - управление компонентом Outline.
    ///
    /// Требует наличия компонента Outline на том же GameObject.
    /// </summary>
    [RequireComponent(typeof(Outline))]
    public class InteractableHighlighter : NetworkBehaviour, IHighlightable
    {
        [Header("Outline Settings")]
        [Tooltip("Цвет обводки, когда объект доступен для взаимодействия.")]
        [SerializeField] private Color availableColor = new Color(1f, 1f, 1f, 1f);

        [Tooltip("Цвет обводки, когда игрок навелся на объект (фокус).")]
        [SerializeField] private Color focusColor = new Color(1f, 0.9f, 0f, 1f);

        [Tooltip("Толщина обводки.")]
        [SerializeField, Range(0f, 10f)] private float availableOutlineWidth = 2f;
        [SerializeField, Range(0f, 10f)] private float focusOutlineWidth = 6f;

        [Header("Dependencies")]
        [Tooltip("Interactable компонент объекта.")]
        [SerializeField] private InteractableBase _interactable;

        [Header("Availability Settings")]
        [Tooltip("Интервал проверки доступности объекта в секундах.")]
        [SerializeField, Min(0.05f)] private float availabilityCheckInterval = 0.5f;

        private float _checkTimer;

        /// <summary>
        /// Сетевое состояние доступности объекта.
        /// Видно всем клиентам. Устанавливается только сервером.
        /// </summary>
        private NetworkVariable<bool> _availableHighlight = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// Локальное состояние фокуса.
        /// Не синхронизируется по сети.
        /// </summary>
        private bool _focusHighlight;

        [SerializeField] private Outline _outline;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if(_interactable == null)
                _interactable = GetComponent<InteractableBase>();
            if (_interactable == null)
            {
                Debug.LogError($"[{GetType().Name}] Component _interactable not found on {gameObject.name}!");
                return;
            }

            if (_outline == null )
                _outline = GetComponent<Outline>();
            if (_outline == null)
            {
                Debug.LogError($"[{GetType().Name}] Component Outline not found on {gameObject.name}!");
                return;
            }

            // Настраиваем базовые параметры Outline
            _outline.OutlineWidth = availableOutlineWidth;
            _outline.OutlineMode = Outline.Mode.OutlineAll; // Или другой режим по желанию

            // Подписываемся на изменение сетевого состояния доступности.
            _availableHighlight.OnValueChanged += HandleAvailableChanged;

            // По умолчанию выключаем обводку
            UpdateOutlineState();

            if (IsServer)
            {
                RefreshAvailability();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_availableHighlight != null)
            {
                _availableHighlight.OnValueChanged -= HandleAvailableChanged;
            }

            base.OnNetworkDespawn();
        }
        private void Update()
        {
            // Проверка доступности выполняется только на сервере,
            // потому что подсветка доступности (Outline) синхронизируется всем клиентам.
            if (!IsSpawned || !IsServer)
                return;

            _checkTimer += Time.deltaTime;

            if (_checkTimer < availabilityCheckInterval)
                return;

            _checkTimer = 0f;

            RefreshAvailability();
        }

        /// <summary>
        /// Устанавливает сетевую подсветку доступности.
        /// Работает только на сервере.
        /// </summary>
        public void SetAvailableHighlight(bool state)
        {
            if (!IsSpawned || !IsServer)
                return;

            if (_availableHighlight.Value == state)
                return;

            _availableHighlight.Value = state;
        }

        /// <summary>
        /// Устанавливает локальную подсветку фокуса.
        /// Не синхронизируется по сети.
        /// </summary>
        public void SetFocusHighlight(bool state)
        {
            if (!IsSpawned)
                return;
            Debug.Log($"[InteractionAddon] SetFocusHighlight({state}) for");

            if (_focusHighlight == state)
                return;

            _focusHighlight = state;

            UpdateOutlineState();
        }

        /// <summary>
        /// Обработчик изменения сетевого состояния доступности.
        /// </summary>
        private void HandleAvailableChanged(bool previousValue, bool newValue)
        {
            UpdateOutlineState();
        }

        /// <summary>
        /// Обновляет визуальное состояние компонента Outline на основе текущих флагов.
        /// Приоритет: Фокус > Доступность.
        /// </summary>
        private void UpdateOutlineState()
        {
            if (_outline == null)
                return;

            bool shouldEnable = _focusHighlight || _availableHighlight.Value;

            _outline.enabled = shouldEnable;

            if (shouldEnable)
            {
                // Фокус имеет более высокий визуальный приоритет
                _outline.OutlineColor = _focusHighlight ? focusColor : availableColor;
                _outline.OutlineWidth = _focusHighlight ? focusOutlineWidth : availableOutlineWidth;
            }
        }

        /// <summary>
        /// Обновляет состояние доступности объекта.
        /// </summary>
        private void RefreshAvailability()
        {
            if (_interactable == null)
            {
                SetAvailableHighlight(false);
                return;
            }

            bool available = _interactable.HasAnyAvailableInteractor();

            SetAvailableHighlight(available);
        }
    }
}