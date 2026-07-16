using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Контроллер 3D прогресс-бара обратного отсчета до взрыва сломанного автомата.
    /// Отвечает ТОЛЬКО за заполнение и цвет шкалы.
    /// Видимостью управляет SlotMachineCountdownIndicator.
    /// Работает только на клиентах (не на сервере).
    /// </summary>
    public class SlotMachineCountdownController : MonoBehaviour
    {
        [Header("Ссылки")]
        [Tooltip("Прогресс-бар для отображения обратного отсчета")]
        [SerializeField] private ProgressBar3D progressBar;

        [Tooltip("Ядро автомата, время которого мы отслеживаем")]
        [SerializeField] private SlotMachine slotMachine;

        [Header("Настройки цветов")]
        [Tooltip("Цвет при большом оставшемся времени (>66%)")]
        [SerializeField] private Color safeColor = Color.green;

        [Tooltip("Цвет при среднем оставшемся времени (33-66%)")]
        [SerializeField] private Color warningColor = Color.yellow;

        [Tooltip("Цвет при малом оставшемся времени (<33%)")]
        [SerializeField] private Color dangerColor = Color.red;

        [Header("Настройки фона")]
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        [Header("Настройки анимации")]
        [Tooltip("Скорость плавного изменения заполнения")]
        [SerializeField] private float fillAnimationSpeed = 10f;

        [Tooltip("Начальное время обратного отсчета в секундах (должно совпадать с настройкой менеджера)")]
        [SerializeField] private float maxTimeToExplode = 20f;

        private float targetFillAmount = 0f;
        private float currentFillAmount = 0f;

        private void Awake()
        {
            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<ProgressBar3D>();
                if (progressBar == null)
                {
                    Debug.LogError($"[{GetType().Name}] ProgressBar3D не найден на {gameObject.name}");
                    return;
                }
            }

            if (slotMachine == null)
            {
                slotMachine = GetComponentInParent<SlotMachine>();
                if (slotMachine == null)
                {
                    Debug.LogError($"[{GetType().Name}] SlotMachine не найден на {gameObject.name}");
                    return;
                }
            }
        }

        private void OnEnable()
        {
            if (slotMachine != null)
            {
                slotMachine.OnTimeToExplodeChanged += HandleTimeToExplodeChanged;
                slotMachine.OnSlotMachineBreakdownChanged += HandleSlotMachineStateChanged;
                slotMachine.OnSlotMachineExplosionChanged += HandleExplosionStateChanged;

                HandleTimeToExplodeChanged(slotMachine.TimeToExplode);
            }
        }

        private void OnDisable()
        {
            if (slotMachine != null)
            {
                slotMachine.OnTimeToExplodeChanged -= HandleTimeToExplodeChanged;
                slotMachine.OnSlotMachineBreakdownChanged -= HandleSlotMachineStateChanged;
                slotMachine.OnSlotMachineExplosionChanged -= HandleExplosionStateChanged;
            }
        }

        private void Update()
        {
            // Плавная анимация заполнения через Lerp
            currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * fillAnimationSpeed);
            progressBar.SetFill(currentFillAmount);

            // Обновляем цвет в зависимости от оставшегося времени
            Color fillColor = GetColorForTime(currentFillAmount);
            progressBar.SetColors(backgroundColor, fillColor);
        }

        /// <summary>
        /// Обработчик изменений времени до взрыва от ядра автомата.
        /// </summary>
        private void HandleTimeToExplodeChanged(float timeToExplode)
        {
            targetFillAmount = Mathf.Clamp01(timeToExplode / maxTimeToExplode);
        }

        private void HandleSlotMachineStateChanged(bool isBroken)
        {
            if (!isBroken) ResetProgress();
        }

        private void HandleExplosionStateChanged(bool isExploded)
        {
            if (!isExploded) ResetProgress(); // Обработка логики Restore
        }

        private void ResetProgress()
        {
            targetFillAmount = 0f;
            currentFillAmount = 0f;
            progressBar.SetFill(0f);
        }

        /// <summary>
        /// Возвращает цвет для текущего уровня заполнения (обратный отсчет).
        /// 1.0 (много времени) = зеленый, 0.0 (мало времени) = красный.
        /// </summary>
        private Color GetColorForTime(float normalizedTime)
        {
            if (normalizedTime > 0.66f)
            {
                float t = (1f - normalizedTime) / 0.34f;
                return Color.Lerp(safeColor, warningColor, t);
            }
            else if (normalizedTime > 0.33f)
            {
                float t = (0.66f - normalizedTime) / 0.33f;
                return Color.Lerp(warningColor, dangerColor, t);
            }
            else
            {
                return dangerColor;
            }
        }

        /// <summary>
        /// Обработчик события восстановления автомата.
        /// Сбрасывает прогресс-бар.
        /// </summary>
        private void HandleRestored()
        {
            targetFillAmount = 0f;
            currentFillAmount = 0f;
            progressBar.SetFill(0f);
        }
    }
}