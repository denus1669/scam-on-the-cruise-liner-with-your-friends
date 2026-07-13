using System;
using Unity.Netcode;
using UnityEngine;
using Blocks.Gameplay.Core;

/// <summary>
/// Ядро игрового автомата.
/// Хранит сетевое состояние, принимает команды от Менеджера (поломка) 
/// и от Интерактивного компонента (починка игроком).
/// </summary>
public class SlotMachine : NetworkBehaviour
{
    [Header("Идентификатор автомата")]
    public string machineName = "Slot Machine";

    // Единственный источник истины состояния автомата. Записывать может только сервер.
    private readonly NetworkVariable<bool> _isBroken = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private SlotMachineBreakdownManager manager;

    // Свойство для проверки состояния автомата (например, для Interactable или других систем)
    public bool IsBroken => _isBroken.Value;

    // Событие для визуалов (лампочки, анимации), на которое подписан SlotsAnimationIndicator
    public event Action<bool> OnSlotMachineStateChanged;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isBroken.OnValueChanged += HandleSlotMachineStateChanged;

        // Синхронизируем состояние визуалов при спавне для всех клиентов
        OnSlotMachineStateChanged?.Invoke(_isBroken.Value);
    }

    public override void OnNetworkDespawn()
    {
        _isBroken.OnValueChanged -= HandleSlotMachineStateChanged;
        base.OnNetworkDespawn();
    }

    private void HandleSlotMachineStateChanged(bool previousValue, bool current)
    {
        OnSlotMachineStateChanged?.Invoke(current);
    }

    /// <summary>
    /// Инициализация. Вызывается менеджером только на сервере.
    /// </summary>
    public void Initialize(SlotMachineBreakdownManager slotManager)
    {
        manager = slotManager;
        if (IsServer)
        {
            _isBroken.Value = false;
        }
    }

    /// <summary>
    /// Вызывается менеджером, когда срабатывает таймер поломки.
    /// Строго серверная логика.
    /// </summary>
    public void BreakDown()
    {
        if (!IsServer || _isBroken.Value) return;

        _isBroken.Value = true;
        Debug.Log($"<color=red>[ПОЛОМКА]</color> Игровой автомат '{machineName}' сломался! Требуется починка.");
    }

    /// <summary>
    /// RPC для запроса починки от клиента. Вызывается из SlotMachineInteractable.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void TryFixMachineServerRpc(ulong clientId)
    {
        if (!_isBroken.Value) return; // Защита: если уже починили, ничего не делаем

        _isBroken.Value = false;
        Debug.Log($"<color=green>[ПОЧИНКА]</color> Игровой автомат '{machineName}' успешно починен игроком (Client ID: {clientId})!");
    }

    [ClientRpc]
    private void NotifySlotMachineStateChangedClientRpc(string animationTrigger)
    {

    }
}