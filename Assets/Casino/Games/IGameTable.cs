using Blocks.Gameplay.Core;
using Unity.Netcode;

public interface IGameTable : IInteractable
{
    // Базовые свойства
    bool IsAvailable { get; } // Свободен ли стол
    GameType GameType { get; } // Тип игры (BlackGreg, Roulette, Poker и т.д.)

    // Методы для управления ботом
    bool CanBotJoin(BotAgent bot);
    void AssignBot(BotAgent bot);
    void RemoveBot();

    // Получить менеджер игры (BlackGregManager, RouletteManager и т.д.)
    NetworkBehaviour GetGameManager();
}