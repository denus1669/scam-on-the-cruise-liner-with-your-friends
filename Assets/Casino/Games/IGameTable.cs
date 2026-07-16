using System;
using Unity.Netcode;
using UnityEngine;

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

    /// <summary>Тип стола.</summary>
    string TableType { get; }
    /// <summary>Место бота за столом.</summary>
    Transform BotWaitPoint { get; }

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

    /// <summary>Завершить игру. Вызывается только на сервере.</summary>
    void EndGame();

    /// <summary>
    /// Экстренно останавливает игру (например, при поимке читера за руку).
    /// </summary>
    /// <param name="winnerClientId">ID клиента победителя (или ulong.MaxValue, если победил банк/бот).</param>
    /// <param name="isCheaterBot">Указывает, был ли пойманный читер ботом.</param>
    /// <param name="reason">Причина остановки (для логов и UI).</param>
    void ForceStopGame(ulong winnerClientId, bool isCheaterBot, string reason);


    /// <summary>
    /// Вызывается сервером, когда читер пойман за руку.
    /// Каждый стол сам решает, что делать: остановить игру, откатить состояние или наложить штраф.
    /// </summary>
    /// <param name="accuserClientId">ID игрока, который поймал читера.</param>
    /// <param name="isCheaterBot">true, если читер — бот, false — если игрок.</param>
    void OnCheaterCaught(ulong accuserClientId, bool isCheaterBot);

    bool CanAssignBot();
}