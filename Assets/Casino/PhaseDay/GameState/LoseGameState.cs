using UnityEngine;

public sealed class LoseGameState : GameStateBase
{
    public LoseGameState(GameSessionManager gameSessionManager) : base(gameSessionManager) { }
    public override GameState Type => GameState.LoseGame;
    public override void Enter()
    {
        Debug.Log("Lose");
        Debug.Log("Lose");
        Debug.Log("Lose");
        Debug.Log("Lose");
        Debug.Log("Lose");
        Debug.Log("Lose");
        Debug.Log("Lose");
        Debug.Log("Lose");
    }
    public override void OnContinuePressed() => gameSessionManager.SetGameState(GameState.GameStatistic);
}

