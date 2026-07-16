using System;
using Unity.Netcode;
using UnityEngine;
using Blocks.Gameplay.Core;

/// <summary>
/// Ядро игрового автомата.
/// Хранит сетевое состояние, принимает команды от Менеджера (поломка, взрыв) 
/// и от Интерактивного компонента (починка игроком).
/// </summary>
public class SlotMachine : NetworkBehaviour
{
    [Header("Идентификатор автомата")]
    public string machineName = "Slot Machine";

    // Состояние поломки - источник истины. Записывать может только сервер.
    private readonly NetworkVariable<bool> _isBroken = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Состояние взрыва - автомат уничтожен и больше не функционирует
    private readonly NetworkVariable<bool> _isExploded = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Сетевая переменная времени до взрыва. Обновляется сервером.
    private readonly NetworkVariable<float> _timeToExplode = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private SlotMachineBreakdownManager manager;

    // Свойства для проверки состояния автомата
    public bool IsBroken => _isBroken.Value;
    public bool IsExploded => _isExploded.Value;

    // Свойство для доступа
    public float TimeToExplode => _timeToExplode.Value;

    // Событие для визуальных компонентов (обратный отсчет прогресс-бара)
    public event Action<float> OnTimeToExplodeChanged;
    // Добавьте новое событие для уведомлений о восстановлении
    public event Action OnSlotMachineRestored;

    // Добавьте новый метод-обработчик:
    private void HandleTimeToExplodeChanged(float previousValue, float newValue)
    {
        OnTimeToExplodeChanged?.Invoke(newValue);
    }

    // Добавьте метод для установки времени (вызывается менеджером):
    /// <summary>
    /// Устанавливает оставшееся время до взрыва. Вызывается только сервером.
    /// </summary>
    public void SetTimeToExplode(float time)
    {
        if (!IsServer) return;
        _timeToExplode.Value = Mathf.Max(0f, time);
    }


    // Событие для визуалов (лампочки, анимации поломки)
    public event Action<bool> OnSlotMachineStateChanged;

    // Событие для взрыва (VFX, звук, тряска камеры)
    public event Action OnSlotMachineExploded;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isBroken.OnValueChanged += HandleSlotMachineStateChanged;
        _isExploded.OnValueChanged += HandleExplosionStateChanged;
        _timeToExplode.OnValueChanged += HandleTimeToExplodeChanged;

        OnTimeToExplodeChanged?.Invoke(_timeToExplode.Value);
        // Синхронизируем состояние визуалов при спавне для всех клиентов
        OnSlotMachineStateChanged?.Invoke(_isBroken.Value);
    }

    public override void OnNetworkDespawn()
    {
        _isBroken.OnValueChanged -= HandleSlotMachineStateChanged;
        _isExploded.OnValueChanged -= HandleExplosionStateChanged;
        _timeToExplode.OnValueChanged -= HandleTimeToExplodeChanged;

        base.OnNetworkDespawn();
    }

    private void HandleSlotMachineStateChanged(bool previousValue, bool current)
    {
        OnSlotMachineStateChanged?.Invoke(current);
    }

    private void HandleExplosionStateChanged(bool previousValue, bool current)
    {
        if (current)
        {
            OnSlotMachineExploded?.Invoke();
        }
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
            _isExploded.Value = false;
        }
    }

    /// <summary>
    /// Вызывается менеджером, когда срабатывает таймер поломки.
    /// Строго серверная логика.
    /// </summary>
    public void BreakDown()
    {
        if (!IsServer) return;

        // Не ломаем уже сломанные или взорванные автоматы
        if (_isBroken.Value || _isExploded.Value) return;

        _isBroken.Value = true;
        Debug.Log($"<color=red>[ПОЛОМКА]</color> Игровой автомат '{machineName}' сломался! Требуется починка.");
    }

    /// <summary>
    /// Вызывается менеджером, когда автомат не починили вовремя и он взрывается.
    /// Строго серверная логика. Это финальное состояние - автомат больше не работает.
    /// </summary>
    public void Explode()
    {
        if (!IsServer) return;
        if (_isExploded.Value) return;

        // Автомат взрывается независимо от того, сломан он или нет
        _isExploded.Value = true;
        _isBroken.Value = false; // Снимаем состояние поломки, теперь он взорван

        HandleExplosionStateChanged(false, true);
        Debug.Log($"<color=red>[ВЗРЫВ]</color> Игровой автомат '{machineName}' взорвался! Все боты в локации получили раздражение.");
    }



    /// <summary>
    /// RPC для запроса починки от клиента. Вызывается из SlotMachineInteractable.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void TryFixMachineServerRpc(ulong clientId)
    {
        // Нельзя починить взорванный автомат
        if (_isExploded.Value)
        {
            Debug.LogWarning($"[ПОЧИНКА] Автомат '{machineName}' взорван и не может быть починен!");
            return;
        }

        if (!_isBroken.Value) return; // Защита: если уже починили, ничего не делаем

        _isBroken.Value = false;
        Debug.Log($"<color=green>[ПОЧИНКА]</color> Игровой автомат '{machineName}' успешно починен игроком (Client ID: {clientId})!");
    }

    /// <summary>
    /// Восстанавливает автомат после взрыва.
    /// Сбрасывает состояние взрыва и время до взрыва.
    /// Вызывается только на сервере после завершения всех эффектов.
    /// </summary>
    public void Restore()
    {
        if (!IsServer) return;
        if (!_isExploded.Value) return;

        _isExploded.Value = false;
        _timeToExplode.Value = 0f;

        Debug.Log($"<color=cyan>[ВОССТАНОВЛЕНИЕ]</color> Игровой автомат '{machineName}' восстановлен и готов к работе.");

        OnSlotMachineRestored?.Invoke();
    }

    /// <summary>
    /// Сброс всех состояний автомата (для начала нового раунда или перезапуска).
    /// Вызывается только на сервере.
    /// </summary>
    public void Reset()
    {
        if (!IsServer) return;

        _isBroken.Value = false;
        _isExploded.Value = false;
        _timeToExplode.Value = 0f;

        Debug.Log($"[СБРОС] Игровой автомат '{machineName}' сброшен в исходное состояние.");

        OnSlotMachineRestored?.Invoke();
    }

    [ClientRpc]
    private void NotifySlotMachineStateChangedClientRpc(string animationTrigger)
    {
        // Заглушка для будущих анимаций
    }
}