using System;
using Unity.Netcode;

/// <summary>
/// Базовый интерфейс игрового стола.
/// Предоставляет состояние занятости игроком и ботом, события жизненного цикла и методы управления.
/// </summary>
public interface IGameTable
{
    /// <summary>Занят ли стол игроком в данный момент.</summary>
    bool IsOccupied { get; }

    /// <summary>ID клиента, занявшего стол (ulong.MaxValue, если свободен).</summary>
    ulong OccupiedByClientId { get; }

    /// <summary>Занято ли место бота за этим столом.</summary>
    bool IsBotOccupied { get; }

    /// <summary>Идёт ли сейчас игра за этим столом.</summary>
    bool IsGameStarted { get; }

    /// <summary>Срабатывает при смене владельца стола. Передаётся новый OccupiedByClientId или ulong.MaxValue при освобождении.</summary>
    event Action<ulong> OnOccupantChanged;

    /// <summary>Срабатывает при изменении занятости бота. Параметр: true — бот занял место, false — освободил.</summary>
    event Action<bool> OnBotOccupancyChanged;

    /// <summary>Срабатывает, когда игра началась (gameInProgress стал true).</summary>
    event Action OnGameStarted;

    /// <summary>Срабатывает, когда игра завершилась (gameInProgress стал false).</summary>
    event Action OnGameEnded;

    /// <summary>Занять стол игроком. Вызывается только на сервере.</summary>
    /// <param name="clientId">ID клиента, который занимает стол.</param>
    void Occupy(ulong clientId);

    /// <summary>Освободить стол от игрока. Вызывается только на сервере.</summary>
    /// <param name="clientId">ID клиента, который покидает стол.</param>
    void Leave(ulong clientId);

    /// <summary>Назначить бота за стол. Вызывается только на сервере.</summary>
    /// <param name="bot">NetworkObject бота, который будет привязан к столу.</param>
    void AssignBot(NetworkObject bot);

    /// <summary>Убрать бота из-за стола. Вызывается только на сервере.</summary>
    void RemoveBot();

    /// <summary>Начать игру. Вызывается только на сервере.</summary>
    void StartGame();

    /// <summary>Принудительно завершить игру. Вызывается только на сервере.</summary>
    void EndGame();
}