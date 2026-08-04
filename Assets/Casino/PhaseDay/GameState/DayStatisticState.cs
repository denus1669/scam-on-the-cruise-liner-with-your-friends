using UnityEngine;

public sealed class DayStatisticState : GameStateBase
{
    private bool _continuePressed;
    private float _afterContinue;
    public DayStatisticState(GameSessionManager gameSessionManager) : base(gameSessionManager) { }
    public override GameState Type => GameState.DayStatistic;

    public override void OnContinuePressed() => _continuePressed = true;

    public override void Tick(float dt)
    {
        if (!_continuePressed) return;
        if ((_afterContinue += dt) < 2f) return; // пауза для UX

        if (!gameSessionManager.IsLastDay)
        {
            gameSessionManager.AdvanceDay();
            gameSessionManager.SetGameState(GameState.Preparing);
        }
        else
        {
            gameSessionManager.SetGameState(gameSessionManager.IsSessionWon ? GameState.WinGame : GameState.LoseGame);
        }
    }
}