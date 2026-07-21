using System;
using Unity.Netcode;
using UnityEngine;
using Blocks.Gameplay.Core;
using System.Collections;

/// <summary>
/// Выигрышная комбинация для слот-машины.
/// </summary>
[Serializable]
public struct SlotCombo
{
    [Tooltip("Уникальный ID комбинации")]
    public int comboId;

    [Tooltip("Название для отладки (например '777', 'Cherry Cherry Cherry')")]
    public string displayName;
}

/// <summary>
/// Ядро игрового автомата.
/// Хранит сетевое состояние, принимает команды от Менеджера (поломка, взрыв) 
/// и от Интерактивного компонента (починка игроком).
/// </summary>
public class SlotMachine : GameTable
{
    [Header("Игровая логика спина")]
    [Tooltip("Шанс выигрыша (0.10 = 10%)")]
    [SerializeField] private float winChance = 0.10f;

    [Tooltip("Длительность анимации спина в секундах")]
    [SerializeField] private float spinDuration = 4f;

    [Tooltip("Выигрышные комбинации")]
    [SerializeField] private SlotCombo[] winningCombos;

    [Header("Идентификатор автомата")]
    [SerializeField] public string slotMachineName;

    // Состояние спина - крутится ли автомат прямо сейчас
    private readonly NetworkVariable<bool> _isSpinning = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

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

    // [ИСПРАВЛЕНО] Время до взрыва теперь обычная переменная только для сервера
    private float _serverTimeToExplode;

    private SlotMachineBreakdownManager manager;

    // Свойства для проверки состояния автомата
    public bool IsBroken => _isBroken.Value;
    public bool IsExploded => _isExploded.Value;
    public float TimeToExplode => _serverTimeToExplode; // Актуально только на сервере
    public override string TableType => "SlotMachine";
    public bool IsSpinning => _isSpinning.Value;

    // Событие для подписки ботом и другими системами
    public event Action<float> OnSpinStarted; // duration
    public event Action<bool, int> OnSpinCompleted; // (isWin, comboIndex или -1)

    // [ИСПРАВЛЕНО] Событие для таймера. Передаем double (ServerTime), чтобы клиент сам тикал локально
    public event Action<double> OnTimeToExplodeStarted;

    // События для визуалов (лампочки, анимации поломки)
    public event Action<bool> OnSlotMachineBreakdownChanged; // true = сломан, false = исправен
    public event Action<bool> OnSlotMachineExplosionChanged; // true = взорван, false = исправен

    #region GameTable Overrides

    public override void StartGame()
    {
        if (!IsServer)
        {
            Debug.LogWarning($"[SlotMachine] Попытка начать игру на клиенте. Игровая логика должна выполняться только на сервере.");
            return;
        }

        if (!CanStartGame())
        {
            Debug.LogWarning($"[SlotMachine] Бот не может начать игру.");
            return;
        }

        gameInProgress.Value = true;
        Debug.Log($"[SlotMachine] Игра на автомате '{slotMachineName}' началась. Бот: {currentBot?.name}");
    }

    protected override bool CanStartGame()
    {
        if (!IsBotOccupied)
        {
            Debug.LogWarning($"[SlotMachine] Невозможно начать игру: Бот не назначен.");
            return false;
        }
        return true;
    }

    public override bool CanAssignBot()
    {
        // [УЛУЧШЕНО] Заменил Warning на Log, чтобы не спамить в консоль при каждом поиске стола
        bool canAssign = base.CanAssignBot() && !_isBroken.Value && !_isExploded.Value;
        return canAssign;
    }

    #endregion

    /// <summary>
    /// Устанавливает оставшееся время до взрыва. Вызывается только сервером.
    /// </summary>
    public void SetTimeToExplode(float time)
    {
        if (!IsServer) return;
        _serverTimeToExplode = Mathf.Max(0f, time);

        // [ИСПРАВЛЕНО] Используем NetworkManager.ServerTime (встроенное свойство NetworkBehaviour)
        double endTime = NetworkManager.ServerTime.Time + time;
        StartExplosionTimerClientRpc(endTime);
    }

    [ClientRpc]
    private void StartExplosionTimerClientRpc(double serverEndTime)
    {
        // Клиент получает точное серверное время окончания и сам запускает локальный таймер
        OnTimeToExplodeStarted?.Invoke(serverEndTime);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isBroken.OnValueChanged += HandleSlotMachineBreakdownChanged;
        _isExploded.OnValueChanged += HandleExplosionStateChanged;
        _isSpinning.OnValueChanged += HandleSpinningStateChanged;

        // Синхронизируем состояние визуалов при спавне для всех клиентов (включая опоздавших)
        OnSlotMachineBreakdownChanged?.Invoke(_isBroken.Value);
        OnSlotMachineExplosionChanged?.Invoke(_isExploded.Value);

        // [ИСПРАВЛЕНО] Синхронизируем таймер взрыва для опоздавших клиентов
        if (_serverTimeToExplode > 0)
        {
            double endTime = NetworkManager.ServerTime.Time + _serverTimeToExplode;
            OnTimeToExplodeStarted?.Invoke(endTime);
        }
    }

    public override void OnNetworkDespawn()
    {
        // [ИСПРАВЛЕНО] Убраны старые подписки на _timeToExplode, которых больше нет
        _isBroken.OnValueChanged -= HandleSlotMachineBreakdownChanged;
        _isExploded.OnValueChanged -= HandleExplosionStateChanged;
        _isSpinning.OnValueChanged -= HandleSpinningStateChanged;

        base.OnNetworkDespawn();
    }

    private void HandleSlotMachineBreakdownChanged(bool previousValue, bool current)
    {
        Debug.Log($"[SlotMachine] Автомат сломался");
        OnSlotMachineBreakdownChanged?.Invoke(current);
    }

    private void HandleExplosionStateChanged(bool previousValue, bool current)
    {
        Debug.Log($"[SlotMachine] Автомат взорвался");
        OnSlotMachineExplosionChanged?.Invoke(current);
    }

    private void HandleSpinningStateChanged(bool previousValue, bool current)
    {
        if(current)
            OnSpinStarted?.Invoke(spinDuration);
        Debug.Log($"[SlotMachine] Рулетка вращается");
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

    public void BreakDown()
    {
        if (!IsServer) return;
        if (_isBroken.Value || _isExploded.Value) return;

        _isBroken.Value = true;
        RemoveBot();
        Debug.Log($"<color=red>[ПОЛОМКА]</color> Игровой автомат '{slotMachineName}' сломался! Требуется починка.");
    }

    public void Explode()
    {
        if (!IsServer) return;
        if (_isExploded.Value) return;

        _isExploded.Value = true;
        _isBroken.Value = false;

        Debug.Log($"<color=red>[ВЗРЫВ]</color> Игровой автомат '{slotMachineName}' взорвался! Все боты в локации получили раздражение.");
    }

    [Rpc(SendTo.Server)]
    public void TryFixMachineServerRpc(ulong clientId)
    {
        if (_isExploded.Value)
        {
            Debug.LogWarning($"[ПОЧИНКА] Автомат '{slotMachineName}' взорван и не может быть починен!");
            return;
        }

        if (!_isBroken.Value) return;

        Occupy(clientId);
        Debug.Log($"[SlotMachine] Игрок {clientId} начал починку автомата '{slotMachineName}'");

        _isBroken.Value = false;
        Debug.Log($"<color=green>[ПОЧИНКА]</color> Игровой автомат '{slotMachineName}' успешно починен игроком (Client ID: {clientId})!");

        Leave(clientId);
        Debug.Log($"[SlotMachine] Игрок {clientId} завершил починку, стол освобождён");
    }

    public void Restore()
    {
        if (!IsServer) return;
        if (!_isExploded.Value) return;

        _isExploded.Value = false;
        // [ИСПРАВЛЕНО] Обращаемся к локальной переменной сервера
        _serverTimeToExplode = 0f;

        Debug.Log($"<color=cyan>[ВОССТАНОВЛЕНИЕ]</color> Игровой автомат '{slotMachineName}' восстановлен и готов к работе.");
    }

    public void Reset()
    {
        if (!IsServer) return;

        _isBroken.Value = false;
        _isExploded.Value = false;
        // [ИСПРАВЛЕНО] Обращаемся к локальной переменной сервера
        _serverTimeToExplode = 0f;
        _isSpinning.Value = false;

        Debug.Log($"[СБРОС] Игровой автомат '{slotMachineName}' сброшен в исходное состояние.");
    }

    public void Spin()
    {
        if (!IsServer) return;

        if (_isBroken.Value || _isExploded.Value)
        {
            Debug.LogWarning($"[SlotMachine] Автомат '{slotMachineName}' сломан/взорван, спин невозможен");
            return;
        }

        if (_isSpinning.Value)
        {
            Debug.LogWarning($"[SlotMachine] Автомат '{slotMachineName}' уже крутится");
            return;
        }

        if (!gameInProgress.Value)
        {
            Debug.LogWarning($"[SlotMachine] Автомат '{slotMachineName}' не в режиме игры");
            return;
        }

        float roll = UnityEngine.Random.value;
        bool isWin = roll < winChance;

        int comboIndex = -1;
        if (isWin && winningCombos != null && winningCombos.Length > 0)
        {
            comboIndex = UnityEngine.Random.Range(0, winningCombos.Length);
        }

        Debug.LogError($"[SlotMachine] Автомат '{slotMachineName}' начинает спин. Результат: {(isWin ? "ПОБЕДА" : "ПРОИГРЫШ")}");
        StartCoroutine(SpinRoutine(isWin, comboIndex));
    }

    private IEnumerator SpinRoutine(bool isWin, int comboIndex)
    {
        _isSpinning.Value = true;
        PlaySpinAnimationClientRpc(isWin, comboIndex, spinDuration);

        yield return new WaitForSeconds(spinDuration);

        InterpretResult(isWin, comboIndex);
        _isSpinning.Value = false;
    }

    private void InterpretResult(bool isWin, int comboIndex)
    {
        if (isWin) Win(comboIndex);
        else Lose();
    }

    private void Win(int comboIndex)
    {
        string comboName = "Неизвестно";
        if (winningCombos != null && comboIndex >= 0 && comboIndex < winningCombos.Length)
        {
            comboName = winningCombos[comboIndex].displayName;
        }

        Debug.Log($"<color=green>[ПОБЕДА]</color> Автомат '{slotMachineName}' выиграл комбинацию: {comboName}");
        PlayJackpotVFXClientRpc();
        OnSpinCompleted?.Invoke(true, comboIndex);
    }

    private void Lose()
    {
        Debug.Log($"<color=red>[ПРОИГРЫШ]</color> Автомат '{slotMachineName}' проиграл");
        OnSpinCompleted?.Invoke(false, -1);
    }

    [ClientRpc]
    private void PlaySpinAnimationClientRpc(bool isWin, int comboIndex, float duration)
    {
        Debug.Log($"[SlotMachine Client] Анимация спина: isWin={isWin}, combo={comboIndex}, duration={duration}");
        // slotMachineVisuals.PlaySpin(isWin, comboIndex, duration);
    }

    [ClientRpc]
    private void PlayJackpotVFXClientRpc()
    {
        Debug.Log($"[SlotMachine Client] VFX джекпота!");
        // slotMachineVisuals.PlayJackpot();
    }
}