using UnityEngine;

public sealed class WinGameState : GameStateBase
{
    public WinGameState(GameSessionManager gameSessionManager) : base(gameSessionManager) { }
    public override GameState Type => GameState.WinGame;
    public override void OnContinuePressed() => gameSessionManager.SetGameState(GameState.GameStatistic);
}
