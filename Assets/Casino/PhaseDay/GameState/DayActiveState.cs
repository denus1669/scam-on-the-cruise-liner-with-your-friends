using UnityEngine;

public sealed class DayActiveState : GameStateBase
{
    private float _timeRemaining;
    private float _tickAccum;
    private int _spawned;
    private float _nextSpawnIn;

    public DayActiveState(GameSessionManager gameSessionManager) : base(gameSessionManager) { }
    public override GameState Type => GameState.DayActive;

    public override void Enter()
    {
        gameSessionManager.AdvanceDay();
        var dayConfig = gameSessionManager.DayConfiguration;
        _timeRemaining = dayConfig.gamePhaseDuration;
        _tickAccum = 0f;
        _spawned = 0;

        for (int i = 0; i < dayConfig.initialBotCount && _spawned < dayConfig.botCount; i++)
            gameSessionManager.BotSpawner.SpawnBot(_spawned++);

        _nextSpawnIn = Random.Range(dayConfig.spawnDelayMin, dayConfig.spawnDelayMax);
        gameSessionManager.SetTimeRemaining(_timeRemaining);
        gameSessionManager.BreakdownManager.StartBreakdownSession();
    }

    public override void Tick(float dt)
    {
        var dayConfig = gameSessionManager.DayConfiguration;

        _timeRemaining -= dt;
        _tickAccum += dt;
        if (_tickAccum >= 1f) // синхронизируем таймер раз в секунду
        {
            _tickAccum -= 1f;
            gameSessionManager.SetTimeRemaining(Mathf.Max(0, _timeRemaining));
        }

        if (_spawned < dayConfig.botCount)
        {
            _nextSpawnIn -= dt;
            if (_nextSpawnIn <= 0f)
            {
                gameSessionManager.BotSpawner.SpawnBot(_spawned++);
                _nextSpawnIn = Random.Range(dayConfig.spawnDelayMin, dayConfig.spawnDelayMax);
            }
        }

        if (_timeRemaining <= 0f)
            gameSessionManager.SetGameState(GameState.EndDay);
    }
}