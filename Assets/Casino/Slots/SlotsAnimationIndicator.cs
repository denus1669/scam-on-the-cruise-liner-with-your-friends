using UnityEngine;

public class SlotsAnimationIndicator : NetworkObjectVisibilityIndicator
{
    
    [Header("Зависимости")] 
    [SerializeField] private SlotMachine slotMachine;

    protected override void Awake()
    {
        base.Awake();

        if (slotMachine == null)
            slotMachine = GetComponent<SlotMachine>();
    }

    private void OnEnable()
    {
        if (slotMachine != null)
            slotMachine.OnSlotMachineStateChanged += HandleBotStoodChanged;
    }

    private void OnDisable()
    {
        if (slotMachine != null)
            slotMachine.OnSlotMachineStateChanged -= HandleBotStoodChanged;
    }

    private void HandleBotStoodChanged(bool hasStood)
    {
        if (hasStood)
            ShowIndicator();
        else
            HideIndicator();
    }

}
