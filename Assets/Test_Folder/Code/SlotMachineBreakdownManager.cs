using UnityEngine;
using System.Collections.Generic;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Менеджер, управляющий таймерами поломок игровых автоматов в казино.
    /// </summary>
    public class SlotMachineBreakdownManager : MonoBehaviour
    {
        [Header("Список автоматов в казино")]
        [SerializeField] private List<SlotMachineInteractable> slotMachines = new List<SlotMachineInteractable>();

        [Header("Настройки времени поломки (в секундах)")]
        [SerializeField] private float minBreakTime = 30f;
        [SerializeField] private float maxBreakTime = 60f;

        // Внутренний класс для отслеживания состояния таймера каждого автомата
        private class MachineTimerState
        {
            public SlotMachineInteractable machine;
            public float timeRemaining;
        }

        private List<MachineTimerState> m_ActiveTimers = new List<MachineTimerState>();
        private bool m_IsRunning = false;

        private void Start()
        {
            // Запускаем механику при старте игры
            StartBreakdownSession();
        }

        public void StartBreakdownSession()
        {
            if (slotMachines == null || slotMachines.Count == 0)
            {
                Debug.LogWarning($"[{nameof(SlotMachineBreakdownManager)}] Список slotMachines пуст. Добавьте автоматы в Инспекторе!", this);
                return;
            }

            m_ActiveTimers.Clear();

            // Инициализируем каждый автомат и задаем случайный стартовый таймер
            foreach (var machine in slotMachines)
            {
                if (machine == null) continue;

                machine.Initialize(this);

                MachineTimerState timerState = new MachineTimerState
                {
                    machine = machine,
                    timeRemaining = Random.Range(minBreakTime, maxBreakTime)
                };

                m_ActiveTimers.Add(timerState);
                Debug.Log($"[Менеджер поломок] Автомату '{machine.MachineName}' задан таймер: {timerState.timeRemaining:F1} сек.");
            }

            m_IsRunning = true;
        }

        private void Update()
        {
            if (!m_IsRunning) return;

            // Идем по списку таймеров
            for (int i = 0; i < m_ActiveTimers.Count; i++)
            {
                var timerState = m_ActiveTimers[i];

                // Если автомат уже сломан, его собственный таймер поломки временно замораживается
                if (timerState.machine.IsBroken) continue;

                // Уменьшаем оставшееся время
                timerState.timeRemaining -= Time.deltaTime;

                // Если время вышло — автомат ломается
                if (timerState.timeRemaining <= 0f)
                {
                    timerState.machine.TriggerBreakdown();

                    // Правило: Если один из автоматов срабатывает, время сбрасывается на ВСЕХ автоматах и задается заново
                    ResetAllTimers();

                    // Прерываем цикл текущего кадра, так как все таймеры только что обновились
                    break;
                }
            }
        }

        /// <summary>
        /// Сбрасывает текущее время ожидания для всех автоматов и генерирует новые случайные таймеры.
        /// </summary>
        private void ResetAllTimers()
        {
            Debug.Log("<color=yellow>[Менеджер поломок] Один из автоматов вышел из строя! Сброс и перегенерация всех таймеров...</color>");

            foreach (var timerState in m_ActiveTimers)
            {
                timerState.timeRemaining = Random.Range(minBreakTime, maxBreakTime);

                // Выводим лог только для тех, кто прямо сейчас исправен (чтобы понимать новое время ожидания)
                if (!timerState.machine.IsBroken)
                {
                    Debug.Log($"[Менеджер поломок] Автомату '{timerState.machine.MachineName}' обновлен таймер поломки: {timerState.timeRemaining:F1} сек.");
                }
            }
        }

        /// <summary>
        /// Коллбэк, вызываемый автоматом, когда игрок его успешно починил.
        /// </summary>
        public void OnMachineFixed(SlotMachineInteractable fixedMachine)
        {
            // На данном этапе просто фиксируем событие починки. 
            // В будущем сюда можно добавить начисление очков, вызов звуков или запуск QTE мини-игры из диздока.
        }
    }
}