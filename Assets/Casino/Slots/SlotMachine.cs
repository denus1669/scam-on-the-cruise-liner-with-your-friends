using System;
using Unity.Netcode;
using UnityEngine;
using Blocks.Gameplay.Core;

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
    [SerializeField] private float spinDuration = 2f;

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

    public override string TableType => "SlotMachine";

    // Публичное свойство для проверки
    public bool IsSpinning => _isSpinning.Value;

    // Событие для подписки ботом и другими системами
    public event Action<bool, int> OnSpinCompleted; // (isWin, comboIndex или -1)

    // Событие для визуальных компонентов (обратный отсчет прогресс-бара)
    public event Action<float> OnTimeToExplodeChanged;

    // Событие для визуалов (лампочки, анимации поломки)
    public event Action<bool> OnSlotMachineBreakdownChanged; // true = сломан, false = исправен
    public event Action<bool> OnSlotMachineExplosionChanged; // true = взорван, false = исправен

    #region GameTable Overrides
    public override void StartGame()
    {
        if (!IsServer) return;

        if (!CanStartGame())
        {
            Debug.LogWarning($"[SlotMachine] Бот не может начать игру.");
            return;
        }

        gameInProgress.Value = true;
    }

    protected override bool CanStartGame()
    {
        // Базовая проверка: есть ли игрок и бот
        if (!IsBotOccupied)
        {
            Debug.LogWarning($"[SlotMachine] Невозможно начать игру: Бот не назначен.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Переопределение: автомат не должен быть сломан или взорван.
    /// </summary>
    public override bool CanAssignBot()
    {  
        Debug.LogWarning($"[SlotMachine] CanAssignBot={base.CanAssignBot()}, isBroken={_isBroken.Value}, isExploded={_isExploded.Value}");
        return base.CanAssignBot() && !_isBroken.Value && !_isExploded.Value;
    }

    #endregion

    //  метод-обработчик:
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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isBroken.OnValueChanged += HandleSlotMachineStateChanged;
        _isExploded.OnValueChanged += HandleExplosionStateChanged;
        _timeToExplode.OnValueChanged += HandleTimeToExplodeChanged;

        _isSpinning.OnValueChanged += HandleSpinningStateChanged; // НОВОЕ

        OnTimeToExplodeChanged?.Invoke(_timeToExplode.Value);
        // Синхронизируем состояние визуалов при спавне для всех клиентов
        OnSlotMachineBreakdownChanged?.Invoke(_isBroken.Value);
    }

    public override void OnNetworkDespawn()
    {
        _isBroken.OnValueChanged -= HandleSlotMachineStateChanged;
        _isExploded.OnValueChanged -= HandleExplosionStateChanged;
        _timeToExplode.OnValueChanged -= HandleTimeToExplodeChanged;

        _isSpinning.OnValueChanged -= HandleSpinningStateChanged; // НОВОЕ


        base.OnNetworkDespawn();
    }

    private void HandleSlotMachineStateChanged(bool previousValue, bool current)
    {
        Debug.LogWarning($"[SlotMachine] Состояние поломки изменилось: {previousValue} → {current}");
        OnSlotMachineBreakdownChanged?.Invoke(current);
    }

    private void HandleExplosionStateChanged(bool previousValue, bool current)
    {
        Debug.LogWarning($"[SlotMachine] Состояние взрыва изменилось: {previousValue} → {current}");
        OnSlotMachineExplosionChanged?.Invoke(current);
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
        RemoveBot();
        Debug.Log($"<color=red>[ПОЛОМКА]</color> Игровой автомат '{slotMachineName}' сломался! Требуется починка.");
    }

    /// <summary>
    /// Вызывается менеджером, когда автомат не починили вовремя и он взрывается.
    /// Строго серверная логика. Это финальное состояние - автомат больше не работает.
    /// </summary>
    public void Explode()
    {
        if (!IsServer) return;
        if (_isExploded.Value) return;

        
        _isExploded.Value = true;
        _isBroken.Value = false; // Снимаем состояние поломки, теперь он взорван
        RemoveBot();

        Debug.Log($"<color=red>[ВЗРЫВ]</color> Игровой автомат '{slotMachineName}' взорвался! Все боты в локации получили раздражение.");
    }

    /// <summary>
    /// RPC для запроса починки от клиента. Вызывается из SlotMachineInteractable.
    /// </summary>
    [Rpc(SendTo.Server)]
    public void TryFixMachineServerRpc(ulong clientId)
    {
        if (_isExploded.Value)
        {
            Debug.LogWarning($"[ПОЧИНКА] Автомат '{slotMachineName}' взорван и не может быть починен!");
            return;
        }

        if (!_isBroken.Value) return;

        // Занимаем стол игроком на время починки
        Occupy(clientId);
        Debug.Log($"[SlotMachine] Игрок {clientId} начал починку автомата '{slotMachineName}'");

        // Успешная починка
        _isBroken.Value = false;
        Debug.Log($"<color=green>[ПОЧИНКА]</color> Игровой автомат '{slotMachineName}' успешно починен игроком (Client ID: {clientId})!");

        // Освобождаем стол после починки
        Leave(clientId);
        Debug.Log($"[SlotMachine] Игрок {clientId} завершил починку, стол освобождён");

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

        Debug.Log($"<color=cyan>[ВОССТАНОВЛЕНИЕ]</color> Игровой автомат '{slotMachineName}' восстановлен и готов к работе.");

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
        _isSpinning.Value = false; 

        Debug.Log($"[СБРОС] Игровой автомат '{slotMachineName}' сброшен в исходное состояние.");

    }

    public override void RemoveBot()
    {
        if (!IsServer) return;

        currentBot = null;
        botNetworkObjectRef.Value = default;
        isBotOccupied.Value = false;
        Debug.Log($"[GameTable] Бот убран из-за стола.");
    }

    /// <summary>
    /// Запускает спин автомата. Вызывается ботом из SlotsBotBehaviour.
    /// Только серверная логика.
    /// </summary>
    public void Spin()
    {
        if (!IsServer) return;

        // Проверки состояния
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

        // Генерируем результат ЗАРАНЕЕ
        float roll = UnityEngine.Random.value;
        bool isWin = roll < winChance;

        int comboIndex = -1;
        if (isWin && winningCombos != null && winningCombos.Length > 0)
        {
            comboIndex = UnityEngine.Random.Range(0, winningCombos.Length);
        }

        Debug.Log($"[SlotMachine] Автомат '{slotMachineName}' начинает спин. Результат: {(isWin ? "ПОБЕДА" : "ПРОИГРЫШ")}");

        // Запускаем корутину спина
        StartCoroutine(SpinRoutine(isWin, comboIndex));
    }

    /// <summary>
    /// Корутина спина: устанавливает состояние, ждёт анимацию, вызывает результат.
    /// </summary>
    private System.Collections.IEnumerator SpinRoutine(bool isWin, int comboIndex)
    {
        _isSpinning.Value = true;

        // Отправляем ClientRpc для визуализации анимации
        PlaySpinAnimationClientRpc(isWin, comboIndex, spinDuration);

        // Ждём пока анимация завершится
        yield return new WaitForSeconds(spinDuration);

        // Интерпретируем результат
        InterpretResult(isWin, comboIndex);

        _isSpinning.Value = false;
    }

    /// <summary>
    /// Интерпретирует результат спина и вызывает Win() или Lose().
    /// </summary>
    private void InterpretResult(bool isWin, int comboIndex)
    {
        if (isWin)
        {
            Win(comboIndex);
        }
        else
        {
            Lose();
        }
    }
    /// <summary>
    /// Обработка выигрыша. Излучает событие с индексом комбинации.
    /// </summary>
    private void Win(int comboIndex)
    {
        string comboName = "Неизвестно";
        if (winningCombos != null && comboIndex >= 0 && comboIndex < winningCombos.Length)
        {
            comboName = winningCombos[comboIndex].displayName;
        }

        Debug.Log($"<color=green>[ПОБЕДА]</color> Автомат '{slotMachineName}' выиграл комбинацию: {comboName}");

        // VFX джекпота (опционально)
        PlayJackpotVFXClientRpc();

        // Излучаем событие
        OnSpinCompleted?.Invoke(true, comboIndex);
    }

    /// <summary>
    /// Обработка проигрыша.
    /// </summary>
    private void Lose()
    {
        Debug.Log($"<color=red>[ПРОИГРЫШ]</color> Автомат '{slotMachineName}' проиграл");

        // Излучаем событие
        OnSpinCompleted?.Invoke(false, -1);
    }

    [ClientRpc]
    private void PlaySpinAnimationClientRpc(bool isWin, int comboIndex, float duration)
    {
        Debug.Log($"[SlotMachine Client] Анимация спина: isWin={isWin}, combo={comboIndex}, duration={duration}");

        // Здесь будет вызов визуального компонента (шейдер, анимация барабанов)
        // Например: slotMachineVisuals.PlaySpin(isWin, comboIndex, duration);
    }

    [ClientRpc]
    private void PlayJackpotVFXClientRpc()
    {
        Debug.Log($"[SlotMachine Client] VFX джекпота!");

        // Здесь будет вызов VFX (частицы, звук, тряска камеры)
        // Например: slotMachineVisuals.PlayJackpot();
    }

    /// <summary>
    /// Обработчик изменения состояния спина.
    /// </summary>
    private void HandleSpinningStateChanged(bool previousValue, bool current)
    {
        Debug.Log($"[SlotMachine] Состояние спина: {previousValue} → {current}");

        // Можно добавить событие для UI или других систем
        //OnSpinningStateChanged?.Invoke(current);
    }



    [ClientRpc]
    private void NotifySlotMachineStateChangedClientRpc()
    {
    }
}