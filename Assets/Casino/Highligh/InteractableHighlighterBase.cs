using Unity.Netcode;
using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Базовый класс подсветки интерактивных объектов.
    ///
    /// Отвечает за:
    /// - локальную подсветку фокуса;
    /// - сетевую подсветку доступности;
    /// - применение визуала к рендерерам.
    ///
    /// Конкретные наследники сами решают, когда объект считается доступным.
    /// Например:
    /// - DeckHighlighter;
    /// - DoorHighlighter;
    /// - ChestHighlighter.
    /// </summary>
    public abstract class InteractableHighlighterBase : NetworkBehaviour, IHighlightable
    {
        [Header("Renderers")]
        [Tooltip("Рендереры, которые будут подсвечиваться.")]
        [SerializeField] private Renderer[] highlightRenderers;

        [Header("Colors")]
        [Tooltip("Цвет состояния: объект доступен для взаимодействия.")]
        [SerializeField] private Color availableColor = new Color(0f, 1f, 0.25f, 1f);

        [Tooltip("Цвет состояния: игрок навелся на объект.")]
        [SerializeField] private Color focusColor = new Color(1f, 0.9f, 0f, 1f);

        [Tooltip("Интенсивность emission-подсветки.")]
        [SerializeField, Min(0f)] private float emissionIntensity = 2f;

        /// <summary>
        /// Сетевое состояние доступности объекта.
        /// Видно всем клиентам.
        /// Устанавливается только сервером.
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

        private MaterialPropertyBlock _propertyBlock;
        private Color[] _originalColors;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            CacheOriginalColors();

            // Подписываемся на изменение сетевого состояния доступности.
            _availableHighlight.OnValueChanged += HandleAvailableChanged;

            ApplyVisual();
        }

        public override void OnNetworkDespawn()
        {
            // Обязательно отписываемся, чтобы не было утечек и лишних вызовов.
            _availableHighlight.OnValueChanged -= HandleAvailableChanged;

            base.OnNetworkDespawn();
        }

        /// <summary>
        /// Устанавливает сетевую подсветку доступности.
        ///
        /// Важно:
        /// - работает только на сервере;
        /// - видна всем клиентам;
        /// - конкретный наследник сам решает, когда вызывать этот метод.
        /// </summary>
        public void SetAvailableHighlight(bool state)
        {
            if (!IsSpawned)
                return;

            // Доступность объекта является серверно-авторитарной.
            // Если клиент сам попытается изменить доступность, игнорируем.
            if (!IsServer)
                return;

            if (_availableHighlight.Value == state)
                return;

            _availableHighlight.Value = state;
            Debug.Log($"[InteractableHighlighterBase][SetAvailableHighlight] Подсветка доступности сейчас: {_availableHighlight.Value} ");
        }

        /// <summary>
        /// Устанавливает локальную подсветку фокуса.
        ///
        /// Важно:
        /// - не синхронизируется по сети;
        /// - видна только локальному игроку;
        /// - обычно вызывается системой взаимодействия.
        /// </summary>
        public void SetFocusHighlight(bool state)
        {
            if (!IsSpawned)
                return;

            if (_focusHighlight == state)
                return;

            _focusHighlight = state;
            Debug.Log($"[InteractableHighlighterBase][SetAvailableHighlight] Подсветка фокуса сейчас: {_focusHighlight} ");
            ApplyVisual();
        }

        /// <summary>
        /// Обработчик изменения сетевого состояния доступности.
        /// </summary>
        private void HandleAvailableChanged(bool previousValue, bool newValue)
        {
            ApplyVisual();
        }

        /// <summary>
        /// Сохраняет исходные цвета материалов,
        /// чтобы после выключения подсветки можно было вернуть объект в нормальное состояние.
        /// </summary>
        private void CacheOriginalColors()
        {
            if (highlightRenderers == null)
            {
                _originalColors = null;
                return;
            }

            _originalColors = new Color[highlightRenderers.Length];

            for (int i = 0; i < highlightRenderers.Length; i++)
            {
                var renderer = highlightRenderers[i];

                if (renderer == null)
                {
                    _originalColors[i] = Color.white;
                    continue;
                }

                var material = renderer.sharedMaterial;

                if (material == null)
                {
                    _originalColors[i] = Color.white;
                    continue;
                }

                // Пытаемся найти базовый цвет в материале.
                // Сначала проверяем URP-проперти, потом стандартный Built-in проперти.
                if (material.HasProperty(BaseColorId))
                {
                    _originalColors[i] = material.GetColor(BaseColorId);
                }
                else if (material.HasProperty(ColorId))
                {
                    _originalColors[i] = material.GetColor(ColorId);
                }
                else
                {
                    _originalColors[i] = Color.white;
                }
            }
        }

        /// <summary>
        /// Применяет текущее визуальное состояние подсветки.
        ///
        /// Приоритет:
        /// - если есть фокус, показываем цвет фокуса;
        /// - если фокуса нет, но есть доступность, показываем цвет доступности;
        /// - если нет ни фокуса, ни доступности, возвращаем исходный вид.
        /// </summary>
        protected virtual void ApplyVisual()
        {
            if (highlightRenderers == null)
                return;

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            bool active = _focusHighlight || _availableHighlight.Value;

            // Фокус имеет более высокий визуальный приоритет.
            Color targetColor = _focusHighlight
                ? focusColor
                : availableColor;

            for (int i = 0; i < highlightRenderers.Length; i++)
            {
                var renderer = highlightRenderers[i];

                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(_propertyBlock);

                if (active)
                {
                    _propertyBlock.SetColor(BaseColorId, targetColor);
                    _propertyBlock.SetColor(ColorId, targetColor);
                    _propertyBlock.SetColor(EmissionColorId, targetColor * emissionIntensity);
                }
                else
                {
                    Color originalColor = _originalColors != null && i < _originalColors.Length
                        ? _originalColors[i]
                        : Color.white;

                    _propertyBlock.SetColor(BaseColorId, originalColor);
                    _propertyBlock.SetColor(ColorId, originalColor);
                    _propertyBlock.SetColor(EmissionColorId, Color.black);
                }

                renderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}