using UnityEngine;

public class SlotMachine : MonoBehaviour
{
    [Header("Идентификатор автомата")]
    public string machineName = "Slot Machine";

    private bool isBroken = false;
    private SlotMachineManager manager;

    // Свойство для проверки состояния автомата из других скриптов
    public bool IsBroken => isBroken;

    public void Initialize(SlotMachineManager slotManager)
    {
        manager = slotManager;
        isBroken = false;
    }

    /// <summary>
    /// Вызывается менеджером, когда срабатывает таймер поломки.
    /// </summary>
    public void BreakDown()
    {
        if (isBroken) return;

        isBroken = true;
        Debug.Log($"<color=red>[ПОЛОМКА]</color> Игровой автомат '{machineName}' сломался! Требуется починка (Нажмите E).");

        // Здесь в будущем будет запускаться визуальный эффект (искры, дым, пульсация)
    }

    /// <summary>
    /// Метод взаимодействия (вызывается при нажатии клавиши 'E' игроком)
    /// </summary>
    public void Interact()
    {
        if (isBroken)
        {
            FixMachine();
        }
        else
        {
            Debug.Log($"Игровой автомат '{machineName}' исправно работает. Мухлевать пока нельзя.");
        }
    }

    private void FixMachine()
    {
        isBroken = false;
        Debug.Log($"<color=green>[ПОЧИНКА]</color> Игровой автомат '{machineName}' успешно починен игроком!");

        // Оповещаем менеджер, что автомат починен, чтобы он мог возобновить или скорректировать логику, если нужно
        manager.OnMachineFixed(this);
    }
}