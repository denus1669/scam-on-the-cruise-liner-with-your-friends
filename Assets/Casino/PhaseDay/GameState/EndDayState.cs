using UnityEngine;

public sealed class EndDayState : GameStateBase
{
    private float _elapsed;
    public EndDayState(GameSessionManager gameSessionManager) : base(gameSessionManager) { }
    public override GameState Type => GameState.EndDay;

    public override void Enter()
    {
        _elapsed = 0f;
        Debug.Log($"[EndDayState] День {gameSessionManager.CurrentDay} завершается...");

        // 1. Останавливаем все игры
        gameSessionManager.ForceStopAllGames();

        // 2. Останавливаем сессию поломок автоматов
        var breakdownManager = gameSessionManager.BreakdownManager;
        if (breakdownManager != null)
        {
            breakdownManager.StopBreakdownSession();

            // 3. Чиним все автоматы (возвращаем в исходное состояние)
            breakdownManager.RepairAllMachines();
        }

        // 4. Отправляем ботов к выходу
        gameSessionManager.SendBotsToExit();

        // 5. Уведомляем клиентов о завершении дня
        gameSessionManager.NotifyDayEnded(gameSessionManager.CurrentDay);
    }

    public override void Tick(float deltaTime)
    {
        _elapsed += deltaTime;

        // Ждём 5 секунд перед переходом
        if (_elapsed >= 5f)
        {
            Debug.LogWarning($"[EndDayState]  Фишек: {gameSessionManager.Bank.CurrentBalance}, " +
                          $"порог: {gameSessionManager.DayConfiguration.GetChipThresholdForDay(gameSessionManager.CurrentDay)}");
            // Проверка 1: условие проигрыша (фишки ниже поро   га)
            if (gameSessionManager.CheckLoseCondition())
            {
                Debug.Log($"[EndDayState] Проигрыш! Фишек: {gameSessionManager.Bank.CurrentBalance}, " +
                          $"порог: {gameSessionManager.DayConfiguration.GetChipThresholdForDay(gameSessionManager.CurrentDay)}");
                gameSessionManager.SetGameState(GameState.LoseGame);
                return;
            }

            // Проверка 2: это был последний день -> победа
            if (gameSessionManager.IsLastDay)
            {
                Debug.Log($"[EndDayState] Последний день завершён! Победа!");
                gameSessionManager.SetGameState(GameState.WinGame);
                return;
            }
            // Проверка 3: переход к следующему дню
            else
            {
                Debug.Log($"[EndDayState] Переход к подготовке следующего дня...");
                gameSessionManager.SetGameState(GameState.Preparing);
            }
        }
    }
}