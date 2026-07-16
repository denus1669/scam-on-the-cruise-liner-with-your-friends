using UnityEngine;
using Blocks.Gameplay.Core;
using Unity.Netcode;
using System;

/// <summary>
/// Поведение бота для игры на слот-машинах.
/// Бот просто крутит автомат, не принимая сложных решений.
/// При поломке/взрыве ищет новый свободный стол или уходит.
/// </summary>
public class SlotsBotBehaviour : BaseBotBehavior
{
    [Header("Слот-машина")]
    [Tooltip("Текущий автомат, за которым играет бот")]
    private SlotMachine _currentSlot;

    [Header("Настройки поведения")]
    [Tooltip("Задержка между попытками крутить (чтобы не спамить)")]
    [SerializeField] private float spinCooldown = 0.5f;

    private float _lastSpinTime = -999f;

    public override System.Type SupportedTableType => typeof(SlotMachine);

    #region IBotGameBehavior Implementation

    public override void InitializeGame(IGameTable table)
    {
        base.InitializeGame(table);

        if (table is SlotMachine slot)
        {
            _currentSlot = slot;
            Debug.Log($"[SlotsBotBehaviour] Бот инициализирован на автомате '{slot.slotMachineName}'");
        }
        else
        {
            Debug.LogError($"[SlotsBotBehaviour] Передан не SlotMachine! Тип: {table?.GetType().Name}");
        }
    }

    public override void StartSession()
    {
        base.StartSession();

        if (_currentSlot == null) return;

        // Подписываемся на события автомата
        _currentSlot.OnSlotMachineExplosionChanged += HandleSlotExploded;
        _currentSlot.OnSlotMachineBreakdownChanged += HandleSlotBreakdownChanged;
        _currentSlot.OnSpinCompleted += HandleSpinCompleted;

        Debug.Log($"[SlotsBotBehaviour] Бот начал сессию на автомате '{_currentSlot.slotMachineName}'");
    }

    public override void EndSession()
    {
        // Отписываемся от событий
        if (_currentSlot != null)
        {
            _currentSlot.OnSlotMachineExplosionChanged -= HandleSlotExploded;
            _currentSlot.OnSlotMachineBreakdownChanged -= HandleSlotBreakdownChanged;
            _currentSlot.OnSpinCompleted -= HandleSpinCompleted;
            _currentSlot = null;
        }
        base.EndSession();
    }

    #endregion

    #region Game Logic

    /// <summary>
    /// Основная логика действий бота во время игры.
    /// Вызывается из BaseBotBehavior в цикле.
    /// </summary>
    protected override bool EvaluateAndPerformGameAction()
    {
        if (_currentSlot == null)
        {
            Debug.LogWarning("[SlotsBotBehaviour] _currentSlot == null, завершаем сессию");
            return false;
        }

        // Проверка 1: Автомат сломан или взорван → ищем новый или уходим
        if (_currentSlot.IsBroken || _currentSlot.IsExploded)
        {
            Debug.Log($"[SlotsBotBehaviour] Автомат '{_currentSlot.slotMachineName}' сломан/взорван");
            return HandleBrokenSlot();
        }

        // Проверка 2: Автомат уже крутится → ждём
        if (_currentSlot.IsSpinning)
        {
            return true; // Продолжаем сессию, просто ждём
        }

        // Проверка 3: Кулдаун между спинами
        if (Time.time - _lastSpinTime < spinCooldown)
        {
            return true;
        }

        // Проверка 4: Игра всё ещё идёт
        if (!_currentSlot.IsGameStarted)
        {
            Debug.LogWarning("[SlotsBotBehaviour] Игра не начата на автомате");
            return false;
        }

        // Всё ок — крутим автомат!
        _currentSlot.Spin();
        _lastSpinTime = Time.time;

        return true;
    }

    /// <summary>
    /// Обрабатывает ситуацию когда автомат сломан или взорван.
    /// Пытается найти новый свободный стол. Если не может — уходит.
    /// </summary>
    private bool HandleBrokenSlot()
    {
        // Отписываемся от текущего автомата
        if (_currentSlot != null)
        {
            _currentSlot.OnSlotMachineExplosionChanged -= HandleSlotExploded;
            _currentSlot.OnSlotMachineBreakdownChanged -= HandleSlotBreakdownChanged;
            _currentSlot.OnSpinCompleted -= HandleSpinCompleted;
            _currentSlot.RemoveBot();
        }

        // Ищем новый свободный стол через BotAgent
        var botAgent = GetComponent<BotAgent>();
        if (botAgent == null) return false;

        IGameTable newTable = botAgent.FindFreeTable();
        if (newTable != null)
        {
            // Идём к новому столу
            botAgent.GoToTable(newTable);

            // Назначаем себя на новый стол
            newTable.AssignBot(GetComponent<NetworkObject>());

            // Переинициализируемся
            InitializeGame(newTable);
            StartSession();

            return true; // Продолжаем сессию на новом столе
        }

        return false; // Завершаем сессию
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Обработчик поломки автомата.
    /// </summary>
    private void HandleSlotBreakdownChanged(bool isBroken)
    {
        Debug.Log($"[SlotsBotBehaviour] isBroken  {isBroken}");

        if (isBroken)
        {
            Debug.Log($"[SlotsBotBehaviour] Автомат сломался! Бот ищет новый стол...");
            HandleBrokenSlot();
        }
    }

    /// <summary>
    /// Обработчик взрыва автомата.
    /// </summary>
    private void HandleSlotExploded(bool isExploded)
    {
        Debug.Log($"[SlotsBotBehaviour] isExploded {isExploded}");

        if (isExploded)
        {
            Debug.Log($"[SlotsBotBehaviour] Автомат взорвался! Бот идёт к другому столу...");
            HandleBrokenSlot();
        }
    }

    /// <summary>
    /// Обработчик завершения спина.
    /// </summary>
    private void HandleSpinCompleted(bool isWin, int comboIndex)
    {
        if (isWin)
        {
            Debug.Log($"[SlotsBotBehaviour] Бот выиграл! Комбинация #{comboIndex}");
            // Можно добавить анимацию радости, звук и т.д.
        }
        else
        {
            Debug.Log($"[SlotsBotBehaviour] Бот проиграл");
        }
    }

    #endregion
}