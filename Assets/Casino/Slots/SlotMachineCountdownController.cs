using UnityEngine;
using Unity.Netcode;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Контроллер 3D прогресс-бара обратного отсчета до взрыва сломанного автомата.
    /// Отвечает ТОЛЬКО за заполнение и цвет шкалы.
    /// Работает только на клиентах (не на сервере), синхронизируясь через ServerTime.
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

        // Локальные переменные для таймера
        private double _serverEndTime;
        private bool _isCountdownActive;

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
                // [ИЗМЕНЕНО] Подписываемся на новое событие старта таймера
                slotMachine.OnTimeToExplodeStarted += HandleTimeToExplodeStarted;
                slotMachine.OnSlotMachineBreakdownChanged += HandleSlotMachineStateChanged;
                slotMachine.OnSlotMachineExplosionChanged += HandleExplosionStateChanged;
            }
        }

        private void OnDisable()
        {
            if (slotMachine != null)
            {
                slotMachine.OnTimeToExplodeStarted -= HandleTimeToExplodeStarted;
                slotMachine.OnSlotMachineBreakdownChanged -= HandleSlotMachineStateChanged;
                slotMachine.OnSlotMachineExplosionChanged -= HandleExplosionStateChanged;

                _isCountdownActive = false;
            }
        }

        private void Update()
        {
            // Если таймер не активен, не тратим ресурсы на вычисления
            if (!_isCountdownActive) return;

            // [КЛЮЧЕВОЕ ИЗМЕНЕНИЕ] Вычисляем оставшееся время локально на клиенте
            // Используем NetworkManager автомата для получения точного серверного времени
            double currentTime = slotMachine.NetworkManager.ServerTime.Time;
            float remainingTime = (float)(_serverEndTime - currentTime);

            // Если время вышло
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                _isCountdownActive = false; // Останавливаем таймер
            }

            // Обновляем целевое заполнение
            targetFillAmount = Mathf.Clamp01(remainingTime / maxTimeToExplode);

            // Плавная анимация заполнения через Lerp
            currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * fillAnimationSpeed);
            progressBar.SetFill(currentFillAmount);

            // Обновляем цвет в зависимости от оставшегося времени
            Color fillColor = GetColorForTime(currentFillAmount);
            progressBar.SetColors(backgroundColor, fillColor);
        }

        /// <summary>
        /// Обработчик старта таймера. Получает точное серверное время окончания.
        /// </summary>
        private void HandleTimeToExplodeStarted(double serverEndTime)
        {
            _serverEndTime = serverEndTime;
            _isCountdownActive = true;
        }

        private void HandleSlotMachineStateChanged(bool isBroken)
        {
            // Если автомат починили, сбрасываем прогресс-бар
            if (!isBroken) ResetProgress();
        }

        private void HandleExplosionStateChanged(bool isExploded)
        {
            // Если автомат взорвался (или был восстановлен), сбрасываем
            // Примечание: визуальное скрытие/показ самого индикатора лучше делать в SlotMachineCountdownIndicator
            if (!isExploded) ResetProgress();
        }

        private void ResetProgress()
        {
            _isCountdownActive = false;
            targetFillAmount = 0f;
            currentFillAmount = 0f;

            if (progressBar != null)
            {
                progressBar.SetFill(0f);
            }
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
    }
}