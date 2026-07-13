using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Менеджер, управляющий таймерами поломок. 
    /// Работает исключительно на сервере и напрямую дергает методы ядер автоматов (SlotMachine).
    /// </summary>
    public class SlotMachineBreakdownManager : NetworkBehaviour
    {
        [Header("Список автоматов в казино")]
        [SerializeField] private List<SlotMachine> slotMachines = new List<SlotMachine>();

        [Header("Настройки времени поломки (в секундах)")]
        [SerializeField] private float minBreakTime = 30f;
        [SerializeField] private float maxBreakTime = 60f;

        private class MachineTimerState
        {
            public SlotMachine machine;
            public float timeRemaining;
        }

        private List<MachineTimerState> m_ActiveTimers = new List<MachineTimerState>();
        private bool m_IsRunning = false;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Логика поломок работает ТОЛЬКО на сервере
            if (IsServer)
            {
                StartBreakdownSession();
            }
        }

        public void StartBreakdownSession()
        {
            Debug.Log($"[{nameof(SlotMachineBreakdownManager)}] Инициализация менеджера поломок на сервере...");
            if (slotMachines == null || slotMachines.Count == 0)
            {
                Debug.LogWarning($"[{nameof(SlotMachineBreakdownManager)}] Список slotMachines пуст. Добавьте автоматы в Инспекторе!", this);
                return;
            }

            m_ActiveTimers.Clear();

            foreach (var machine in slotMachines)
            {
                Debug.Log($"[{nameof(SlotMachineBreakdownManager)}] Инициализация автомата: {machine.machineName}"); 

                if (machine == null) continue;

                machine.Initialize(this);

                MachineTimerState timerState = new MachineTimerState
                {
                    machine = machine,
                    timeRemaining = Random.Range(minBreakTime, maxBreakTime)
                };

                m_ActiveTimers.Add(timerState);
            }

            m_IsRunning = true;
        }

        private void Update()
        {
            if (!IsServer || !m_IsRunning) return;

            Debug.Log($"[{nameof(SlotMachineBreakdownManager)}] Обновление таймеров поломок...");

            for (int i = 0; i < m_ActiveTimers.Count; i++)
            {
                var timerState = m_ActiveTimers[i];

                if (timerState.machine.IsBroken) continue;

                timerState.timeRemaining -= Time.deltaTime;

                if (timerState.timeRemaining <= 0f)
                {
                    // Менеджер по своей логике дергает ядро
                    timerState.machine.BreakDown();
                    ResetAllTimers();
                    break; // Прерываем цикл, так как мы только что обновили все таймеры
                }
            }
        }

        private void ResetAllTimers()
        {
            Debug.Log("<color=yellow>[Менеджер поломок] Сброс и перегенерация всех таймеров...</color>");

            foreach (var timerState in m_ActiveTimers)
            {
                timerState.timeRemaining = Random.Range(minBreakTime, maxBreakTime);
            }
        }

        /// <summary>
        /// Коллбэк от ядра автомата (SlotMachine) при починке.
        /// </summary>
        public void OnMachineFixed(SlotMachine fixedMachine)
        {
            Debug.Log($"[Менеджер поломок] Сервер зафиксировал починку автомата: {fixedMachine.machineName}");
            // Здесь можно добавить начисление очков, статистику, запуск следующей фазы и т.д.
        }
    }
}