using System.Collections.Generic;

/// <summary>
/// Специализированный интерфейс для карточных игр (блэкджек, покер и т.д.).
/// Предоставляет боту доступ к данным о руках и действиям.
/// </summary>
public interface ICardGameTable : IGameTable
{
    /// <summary>Текущий счёт руки бота.</summary>
    int GetBotScore();

    /// <summary>Текущий счёт руки игрока.</summary>
    int GetPlayerScore();

    /// <summary>Количество карт в руке бота.</summary>
    int GetBotCardCount();

    /// <summary>Безопасная копия списка карт бота (не даёт изменить оригинал).</summary>
    List<CardData> GetBotHandCopy();

    /// <summary>Бот берёт одну карту.</summary>
    void BotDrawCard();

    /// <summary>Бот заканчивает ход (больше не берёт карт).</summary>
    void BotStand();
}