using Assets.Casino.Games;

namespace Assets.Casino.Bot
{

    /// <summary>
    /// Интерфейс для любого игрового поведения бота.
    /// </summary>
    public interface IBotGameBehaviour
    {
        /// <summary>
        /// Инициализирует бота для работы с конкретным столом.
        /// </summary>
        /// <param name="table">Стол, реализующий IGameTable.</param>
        void InitializeGame(IGameTable table);
        void StartSession();
        void EndSession();
    }
}