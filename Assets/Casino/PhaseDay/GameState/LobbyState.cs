using UnityEngine;

public sealed class LobbyState : GameStateBase
{
    public LobbyState(GameSessionManager gameSessionManager) : base(gameSessionManager) { }
    public override GameState Type => GameState.Lobby;
    public override void OnContinuePressed() => gameSessionManager.SetGameState(GameState.Lobby);

}
