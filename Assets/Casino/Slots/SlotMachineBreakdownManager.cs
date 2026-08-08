using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Менеджер, управляющий таймерами поломок и взрывов. 
    /// Работает исключительно на сервере и напрямую дергает методы ядер автоматов (SlotMachine).
    /// При взрыве автомата начисляет раздражение всем ботам в локации.
    /// </summary>
    public class SlotMachineBreakdownManager : NetworkBehaviour
    {
        [Header("Список автоматов в казино")]
        [SerializeField] private List<SlotMachine> slotMachines = new List<SlotMachine>();

        [Header("Настройки времени поломки (в секундах)")]
        [SerializeField] private float minBreakTime = 10f;
        [SerializeField] private float maxBreakTime = 30f;

        [Header("Настройки взрыва")]
        [Tooltip("Время в секундах, которое есть у игроков, чтобы починить автомат до взрыва")]
        [SerializeField] private float timeBeforeExplode = 20f;

        [Tooltip("Количество раздражения, которое получают все боты при взрыве автомата")]
        [SerializeField] private float explodeDispleasureAmount = 50f;


        [Header("Настройки синхронизации")]
        [Tooltip("Как часто обновлять время до взрыва на клиентах (в секундах). Меньше = плавнее, но дороже")]
        [SerializeField] private float syncInterval = 0.1f;

        [Header("Настройки восстановления")]
        [Tooltip("Задержка в секундах перед восстановлением автомата после взрыва (чтобы VFX успели отобразиться)")]
        [SerializeField] private float restoreDelay = 3f;

        private float m_SyncTimer = 0f;

        private class MachineTimerState
        {
            public SlotMachine machine;
            public float timeRemaining;
        }

        private class MachineExplosionTimer
        {
            public SlotMachine machine;
            public float timeUntilExplode;
        }

        private List<MachineTimerState> m_ActiveTimers = new List<MachineTimerState>();
        private List<MachineExplosionTimer> m_PendingExplosions = new List<MachineExplosionTimer>();
        private List<BotDispleasureController> m_CachedBots = new List<BotDispleasureController>();

        private bool m_IsRunning = false;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        /// <summary>
        /// Кэшируем всех ботов на сцене для быстрого доступа при взрывах.
        /// </summary>
        private void CacheAllBots()
        {
            m_CachedBots.Clear();
            var bots = FindObjectsByType<BotDispleasureController>(FindObjectsSortMode.None);
            foreach (var bot in bots)
            {
                if (bot != null)
                {
                    m_CachedBots.Add(bot);
                }
            }
        }

        public void StartBreakdownSession()
        {
            if (slotMachines == null || slotMachines.Count == 0)
            {
                Debug.LogWarning($"[{nameof(SlotMachineBreakdownManager)}] Список slotMachines пуст. Добавьте автоматы в Инспекторе!", this);
                return;
            }

            m_ActiveTimers.Clear();
            m_PendingExplosions.Clear();

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
            }

            m_IsRunning = true;
        }

        private void Update()
        {
            if (!IsServer || !m_IsRunning) return;

            UpdateBreakdownTimers();
            UpdateExplosionTimers();
            UpdateTimeSynchronization();
        }
        /// <summary>
        /// Обновляет таймеры поломок автоматов.
        /// Когда таймер истекает, автомат ломается и добавляется в список ожидающих взрыва.
        /// </summary>
        private void UpdateBreakdownTimers()
        {
            for (int i = 0; i < m_ActiveTimers.Count; i++)
            {
                var timerState = m_ActiveTimers[i];

                // Пропускаем уже сломанные или взорванные автоматы
                if (timerState.machine.IsBroken || timerState.machine.IsExploded) continue;

                timerState.timeRemaining -= Time.deltaTime;

                if (timerState.timeRemaining <= 0f)
                {
                    // Менеджер по своей логике дергает ядро
                    timerState.machine.BreakDown();

                    // Добавляем сломанный автомат в список ожидающих взрыва
                    AddToPendingExplosions(timerState.machine);

                    ResetAllTimers();
                    break; // Прерываем цикл, так как мы только что обновили все таймеры
                }
            }
        }

        /// <summary>
        /// Обновляет таймеры взрывов сломанных автоматов.
        /// Обрабатывает случаи когда автомат починили или взорвался.
        /// </summary>
        private void UpdateExplosionTimers()
        {
            for (int i = m_PendingExplosions.Count - 1; i >= 0; i--)
            {
                var explosionTimer = m_PendingExplosions[i];

                // Проверяем, не был ли автомат уже взорван
                if (explosionTimer.machine.IsExploded)
                {
                    explosionTimer.machine.SetTimeToExplode(0f);
                    m_PendingExplosions.RemoveAt(i);
                    continue;
                }

                // Проверяем, не был ли автомат починен
                if (!explosionTimer.machine.IsBroken)
                {
                    explosionTimer.machine.SetTimeToExplode(0f);
                    m_PendingExplosions.RemoveAt(i);
                    continue;
                }

                // Уменьшаем время до взрыва
                explosionTimer.timeUntilExplode -= Time.deltaTime;

                // Проверяем, не истекло ли время
                if (explosionTimer.timeUntilExplode <= 0f)
                {
                    HandleExplosion(explosionTimer.machine);
                    m_PendingExplosions.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Обрабатывает взрыв автомата: запускает эффект, распределяет раздражение и планирует восстановление.
        /// </summary>
        private void HandleExplosion(SlotMachine machine)
        {
            machine.SetTimeToExplode(0f);
            machine.Explode();
            DistributeDispleasureToAllBots();

            // Запускаем восстановление с задержкой
            StartCoroutine(RestoreMachineWithDelay(machine, restoreDelay));
        }

        /// <summary>
        /// Периодически синхронизирует время до взрыва с клиентами.
        /// Работает с заданным интервалом для баланса между плавностью и нагрузкой на сеть.
        /// </summary>
        private void UpdateTimeSynchronization()
        {
            m_SyncTimer += Time.deltaTime;

            if (m_SyncTimer >= syncInterval)
            {
                m_SyncTimer = 0f;

                foreach (var timer in m_PendingExplosions)
                {
                    if (timer.machine != null && !timer.machine.IsExploded)
                    {
                        timer.machine.SetTimeToExplode(timer.timeUntilExplode);
                    }
                }
            }
        }

        /// <summary>
        /// Добавляет сломанный автомат в список ожидающих взрыва с таймером.
        /// </summary>
        private void AddToPendingExplosions(SlotMachine machine)
        {
            foreach (var timer in m_PendingExplosions)
            {
                if (timer.machine == machine) return;
            }

            MachineExplosionTimer explosionTimer = new MachineExplosionTimer
            {
                machine = machine,
                timeUntilExplode = timeBeforeExplode
            };

            m_PendingExplosions.Add(explosionTimer);

            // ВАЖНО: сразу синхронизируем время с клиентами
            machine.SetTimeToExplode(timeBeforeExplode);

            Debug.Log($"<color=orange>[Менеджер поломок]</color> Автомат '{machine.slotMachineName}' сломан! До взрыва: {timeBeforeExplode} сек.");
        }

        private void ResetAllTimers()
        {
            Debug.Log("<color=yellow>[Менеджер поломок] Сброс и перегенерация всех таймеров...</color>");

            foreach (var timerState in m_ActiveTimers)
            {
                // Не сбрасываем таймеры для взорванных автоматов
                if (!timerState.machine.IsExploded)
                {
                    timerState.timeRemaining = Random.Range(minBreakTime, maxBreakTime);
                }
            }
        }

        /// <summary>
        /// Распределяет раздражение всем ботам в локации при взрыве автомата.
        /// </summary>
        private void DistributeDispleasureToAllBots()
        {

            // Если кэш пуст, пытаемся обновить его
            CacheAllBots();

            if (m_CachedBots.Count == 0)
            {
                Debug.LogWarning("[Менеджер поломок] Не найдено ни одного бота для распределения раздражения!");
                return;
            }


            int affectedBots = 0;
            foreach (var bot in m_CachedBots)
            {
                if (bot != null && bot.IsSpawned)
                {
                    bot.AddInstantDispleasure(explodeDispleasureAmount);
                    affectedBots++;
                }
            }

            Debug.Log($"<color=red>[ВЗРЫВ]</color> Распределено +{explodeDispleasureAmount} раздражения {affectedBots} ботам!");
        }

        /// <summary>
        /// Коллбэк от ядра автомата (SlotMachine) при починке.
        /// Автомат автоматически удаляется из списка взрывов в Update.
        /// </summary>
        public void OnMachineFixed(SlotMachine fixedMachine)
        {
            Debug.Log($"[Менеджер поломок] Сервер зафиксировал починку автомата: {fixedMachine.slotMachineName}");
            // Автомат будет автоматически удален из m_PendingExplosions в следующем кадре Update
            // когда система обнаружит, что machine.IsBroken == false
        }

        /// <summary>
        /// Восстанавливает автомат после задержки, чтобы VFX эффекты успели отобразиться.
        /// </summary>
        private System.Collections.IEnumerator RestoreMachineWithDelay(SlotMachine machine, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (machine != null && machine.IsSpawned)
            {
                machine.Restore();
            }
        }
    

        /// <summary>
        /// Останавливает текущую сессию поломок. 
        /// Вызывается при завершении игрового дня.
        /// </summary>
        public void StopBreakdownSession()
        {
            if (!IsServer) return;

            m_IsRunning = false;
            m_ActiveTimers.Clear();
            m_PendingExplosions.Clear();

            // Сбрасываем таймеры взрыва на клиентах, чтобы UI исчез
            foreach (var machine in slotMachines)
            {
                if (machine != null && machine.IsSpawned)
                {
                    machine.SetTimeToExplode(0f);
                }
            }

            Debug.Log("<color=green>[Менеджер поломок]</color> Сессия поломок остановлена.");
        }

        /// <summary>
        /// Принудительно чинит все автоматы (возвращает в исходное состояние).
        /// Вызывается при завершении игрового дня.
        /// </summary>
        public void RepairAllMachines()
        {
            if (!IsServer) return;
            if (slotMachines == null) return;

            foreach (var machine in slotMachines)
            {
                if (machine == null || !machine.IsSpawned) continue;

                // Чиним и сломанные, и взорванные автоматы
                if (machine.IsBroken || machine.IsExploded)
                {
                    machine.Reset(); 
                }
            }
        }
    }
}