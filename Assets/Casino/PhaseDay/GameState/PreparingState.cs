using UnityEngine;

public sealed class PreparingState : GameStateBase
{
    public PreparingState(GameSessionManager gameSessionManager) : base(gameSessionManager) { }
    public override GameState Type => GameState.Preparing;

    public override void Enter()
    {
        if (ExposureManager.Instance != null)
            ExposureManager.Instance.ApplyStage(ExposureManager.Instance.CurrentLevel);

        Debug.Log($"[Session] День {gameSessionManager.CurrentDay}: подготовка");
    }

    // Разрешён старт только из Preparing — проверка фазы больше не нужна
    public override void OnStartDayRequested() =>
        gameSessionManager.SetGameState(GameState.DayActive);
}