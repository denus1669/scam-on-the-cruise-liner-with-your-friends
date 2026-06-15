using UnityEngine;

/// <summary>
/// Универсальный контекст, который передается в логику мухлежа.
/// Теперь он поддерживает как ботов, так и реальных игроков.
/// </summary>
public class CheatContext
{
    public IGameTable Table { get; private set; }

    /// <summary>Флаг, указывающий, кто мухлюет: ИИ или реальный игрок</summary>
    public bool IsBot { get; private set; }

    // Данные для бота
    public BotAgent Bot { get; private set; }

    // Данные для игрока
    public ulong ClientId { get; private set; }

    /// <summary>Конструктор для БОТА</summary>
    public CheatContext(IGameTable table, BotAgent botAgent)
    {
        Table = table;
        IsBot = true;
        Bot = botAgent;
    }

    /// <summary>Конструктор для ИГРОКА</summary>
    public CheatContext(IGameTable table, ulong clientId)
    {
        Table = table;
        IsBot = false;
        ClientId = clientId;
    }

    /// <summary>Удобный метод для приведения стола к нужному интерфейсу</summary>
    public T GetTableAs<T>() where T : class, IGameTable
    {
        return Table as T;
    }
}