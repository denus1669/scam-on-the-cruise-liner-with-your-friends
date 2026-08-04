using UnityEngine;

public sealed class GameStatisticState : GameStateBase
{
    public GameStatisticState(GameSessionManager gameSessionManager) : base(gameSessionManager) { }
    public override GameState Type => GameState.GameStatistic;
    public override void Enter() => gameSessionManager.NotifySessionEnded();
}