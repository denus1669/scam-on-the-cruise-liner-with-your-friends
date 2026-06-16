using Unity.Netcode;

/// <summary>
/// Интерфейс для любого игрового поведения бота.
/// </summary>
public interface IBotGameBehavior
{
    /// <summary>
    /// Инициализирует бота для работы с конкретным столом.
    /// </summary>
    /// <param name="table">Стол, реализующий IGameTable.</param>
    void InitializeGame(IGameTable table);
    void StartSession();
    void EndSession();
}