using UnityEngine;
using System.Collections.Generic;

public class SlotMachineManager : MonoBehaviour
{
    [Header("Список игровых автоматов")]
    [SerializeField] private List<SlotMachine> slotMachines = new List<SlotMachine>();

    [Header("Настройки таймеров поломки (сек)")]
    [SerializeField] private float minBreakTime = 30f;
    [SerializeField] private float maxBreakTime = 60f;

    // Структура для отслеживания индивидуального таймера каждого автомата
    private class MachineTimer
    {
        public SlotMachine machine;
        public float timeRemaining;
        public bool isTriggered;
    }

    private List<MachineTimer> machineTimers = new List<MachineTimer>();
    private bool isGameActive = false;

    void Start()
    {
        // Автоматически запускаем логику при старте. 
        StartSession();
    }

    public void StartSession()
    {
        if (slotMachines.Count == 0)
        {
            Debug.LogError("[SlotMachineManager] Список автоматов пуст! Добавьте автоматы в инспекторе.");
            return;
        }

        machineTimers.Clear();

        // Инициализируем каждый автомат и задаем ему случайный стартовый таймер
        foreach (var machine in slotMachines)
        {
            if (machine == null) continue;


            MachineTimer timerData = new MachineTimer
            {
                machine = machine,
                timeRemaining = Random.Range(minBreakTime, maxBreakTime),
                isTriggered = false
            };

            machineTimers.Add(timerData);
            Debug.Log($"[Менеджер] Автомату '{machine.machineName}' задан таймер поломки: {timerData.timeRemaining:F1} сек.");
        }

        isGameActive = true;
    }

    void Update()
    {
        if (!isGameActive) return;

        // Кадровая проверка таймеров
        for (int i = 0; i < machineTimers.Count; i++)
        {
            var timerData = machineTimers[i];

            // Если автомат уже сломан, его таймер не идет
            if (timerData.isTriggered || timerData.machine.IsBroken) continue;

            timerData.timeRemaining -= Time.deltaTime;

            // ИСПРАВЛЕНО: Было 0ff, стало 0f
            if (timerData.timeRemaining <= 0f)
            {
                // Автомат ломается!
                timerData.isTriggered = true;
                timerData.machine.BreakDown();

                // По вашему условию: если один из автоматов сработал, время сбрасывается на ВСЕХ автоматах
                ResetAllTimers();
                break; // Выходим из цикла, так как таймеры только что были пересозданы
            }
        }
    }

    /// <summary>
    /// Сбрасывает и задает новые случайные таймеры для всех автоматов
    /// </summary>
    private void ResetAllTimers()
    {
        Debug.Log("<color=yellow>[Менеджер] Один из автоматов сработал! Перезапуск всех таймеров поломки...</color>");

        foreach (var timerData in machineTimers)
        {
            // Назначаем новое случайное время
            timerData.timeRemaining = Random.Range(minBreakTime, maxBreakTime);
            timerData.isTriggered = false;

            if (!timerData.machine.IsBroken)
            {
                Debug.Log($"[Менеджер] Автомату '{timerData.machine.machineName}' обновлен таймер: {timerData.timeRemaining:F1} сек.");
            }
        }
    }

    /// <summary>
    /// Вызывается из SlotMachine, когда игрок успешно починил автомат.
    /// </summary>
    public void OnMachineFixed(SlotMachine fixedMachine)
    {
        // Здесь можно начислить очки или запустить проверку, не починены ли все автоматы.
    }
}