public enum GameState
{
    Lobby, 
    Preparing, 
    DayActive, 
    EndDay,
    DayStatistic, 
    LoseGame, 
    WinGame, 
    GameStatistic
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
}