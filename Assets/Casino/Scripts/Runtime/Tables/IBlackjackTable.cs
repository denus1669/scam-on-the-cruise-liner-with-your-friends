using System;

/// <summary>
/// Контракт стола Блекджек для режима Крупье.
/// Используется клиентским контроллером, UI и будущим ИИ-ботом.
/// </summary>
public interface IBlackjackTable
{
    /// <summary>Событие смены фазы раунда.</summary>
    event Action<BlackjackRoundPhase> OnPhaseChanged;

    /// <summary>Событие изменения шкалы подозрения бота.</summary>
    event Action<float> OnSuspicionChanged;

    /// <summary>Событие обновления состояния стола (таймеры, доступность действий).</summary>
    event Action OnTableStateChanged;

    /// <summary>Текущая фаза раунда.</summary>
    BlackjackRoundPhase CurrentPhase { get; }

    /// <summary>Текущее значение подозрения бота (0..100).</summary>
    float CurrentSuspicion { get; }

    /// <summary>ClientId игрока, назначенного крупье.</summary>
    ulong DealerOwnerId { get; }

    /// <summary>Флаг: активно ли окно для мухлежа.</summary>
    bool IsCheatWindowActive { get; }

    /// <summary>Запрос на занятие места крупье.</summary>
    void AssignDealerServerRpc(ulong clientId);

    /// <summary>Запрос на покидание стола.</summary>
    void LeaveTableServerRpc(ulong clientId);

    /// <summary>Запрос на начало мухлежа. Открывает мини-игру на клиенте.</summary>
    void RequestCheatServerRpc(ulong clientId, DealerCheatType cheatType);

    /// <summary>Отправка результата мини-игры мухлежа на хост для валидации.</summary>
    void SubmitCheatResultServerRpc(ulong clientId, DealerCheatType cheatType, bool success);
}