using UnityEngine;

public sealed class EndDayState : GameStateBase
{
    private float _elapsed;
    public EndDayState(GameSessionManager gameSessionManager) : base(gameSessionManager) { }
    public override GameState Type => GameState.EndDay;

    public override void Enter()
    {
        _elapsed = 0f;
        gameSessionManager.ForceStopAllGames();
        gameSessionManager.SendBotsToExit();
    }

    public override void Tick(float dt)
    {
        if ((_elapsed += dt) >= 5f)
        {
            gameSessionManager.NotifyDayEnded(gameSessionManager.CurrentDay);
            gameSessionManager.SetGameState(GameState.Preparing);
        }
    }
}