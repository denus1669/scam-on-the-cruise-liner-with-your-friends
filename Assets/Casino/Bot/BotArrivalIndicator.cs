using UnityEngine;
using Blocks.Gameplay.Core;

/// <summary>
/// Индикатор, который показывается, когда бот прибыл к столу, 
/// и скрывается, когда начинается игра.
/// Динамически определяет стол, к которому пришел бот.
/// </summary>
public class BotArrivalIndicator : NetworkObjectVisibilityIndicator
{
    [Header("Ссылки")]
    [Tooltip("Агент бота, за которым мы следим")]
    [SerializeField] private BotAgent botAgent;

    // Храним ссылку на стол, на события которого мы сейчас подписаны, чтобы корректно отписаться
    private GameTable _subscribedTable;

    protected override void Awake()
    {
        base.Awake();

        if (botAgent == null)
            botAgent = GetComponentInParent<BotAgent>();
    }

    private void OnEnable()
    {
        if (botAgent != null)
        {
            botAgent.OnArrivedChanged += HandleBotArrived;

            // Если бот УЖЕ прибыл до включения этого объекта (редкий кейс, но возможный)
            if (botAgent.IsArrived)
            {
                SubscribeToCurrentTable();
            }
        }
    }

    private void OnDisable()
    {
        if (botAgent != null)
        {
            botAgent.OnArrivedChanged -= HandleBotArrived;
        }

        // Гарантированная очистка при уничтожении или деактивации
        UnsubscribeFromTable();
    }

    /// <summary>
    /// Срабатывает, когда бот прибыл или ушел от стола.
    /// </summary>
    private void HandleBotArrived(bool hasArrived)
    {
        if (hasArrived)
        {
            SubscribeToCurrentTable();
            ShowIndicator();
        }
        else
        {
            UnsubscribeFromTable();
            HideIndicator();
        }
    }

    /// <summary>
    /// Динамически находим текущий стол бота и подписываемся на его события.
    /// </summary>
    private void SubscribeToCurrentTable()
    {
        // Получаем конкретный стол, за которым сейчас бот
        _subscribedTable = botAgent.CurrentTable as GameTable;

        if (_subscribedTable != null)
        {
            // Подписываемся на начало игры
            _subscribedTable.OnGameStarted += HandleGameStarted;
            _subscribedTable.OnGameEnded += HandleGameEnded; 
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name}] Не удалось получить GameTable из botAgent.CurrentTable");
        }
    }

    /// <summary>
    /// Отписываемся от стола, чтобы не было утечек памяти и ложных срабатываний.
    /// </summary>
    private void UnsubscribeFromTable()
    {
        if (_subscribedTable != null)
        {
            _subscribedTable.OnGameStarted -= HandleGameStarted;
            _subscribedTable.OnGameEnded -= HandleGameEnded; 
            _subscribedTable = null;
        }
    }

    /// <summary>
    /// Срабатывает при начале игры на конкретном столе.
    /// </summary3>
    private void HandleGameStarted()
    {
        HideIndicator();
    }
    
    private void HandleGameEnded()
    {
        HideIndicator();
    }
}