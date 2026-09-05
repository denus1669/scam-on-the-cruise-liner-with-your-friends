public enum GameState
{
    Preparing,    // Подготовка к дню (подключение, выбор стратегии)
    DayActive,    // Игровой день (таймер, боты, игры)
    EndDay,       // Завершение дня (боты уходят, пауза)
    WinGame,      // Победа (последний день завершён успешно)
    LoseGame,     // Проигрыш (условия не выполнены)
    GameStatistic // Итоговая статистика за всю сессию
}

public abstract class GameStateBase
{
    protected readonly GameSessionManager gameSessionManager;
    protected GameStateBase(GameSessionManager gameSessionManager) => this.gameSessionManager = gameSessionManager;

    public abstract GameState Type { get; }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Tick(float dt) { }

    // Команды игроков (приходят из RPC, выполняются только на сервере)
    public virtual void OnStartDayRequested() { }
    public virtual void OnContinuePressed() { }
    public virtual void OnStartSessionRequested() { }
}