using System.Collections;
using UnityEngine;

/// <summary>
/// Специфичное поведение бота для игры BlackGreg (БлэкГрэг).
/// Наследует универсальную логику от BaseBotBehavior.
/// </summary>
public class BlackGregBotBehavior : BaseBotBehaviour
{
    [Header("Специфичные настройки BlackGreg")]
    [Range(0f, 1f)]
    [SerializeField] private float stupidityChance = 0.08f;
    public override System.Type SupportedTableType => typeof(BlackGregTable);

    private ICardGameTable cardTable;

    protected override void Awake()
    {
        base.Awake();
        // Можно добавить специфичные компоненты для блэкджека, если нужны
    }

    public override void InitializeGame(IGameTable table)
    {
        base.InitializeGame(table);

        cardTable = table as ICardGameTable;
        if (cardTable == null)
        {
            Debug.LogError($"[BlackGreg ИИ] Стол {table} не реализует ICardGameTable. Бот не может играть.");
        }
    }

    /// <summary>
    /// Логика принятия решения в Блэкджеке.
    /// </summary>
    protected override bool EvaluateAndPerformGameAction()
    {
        if (cardTable == null) return false;

        int botScore = cardTable.GetBotScore();
        int cardCount = cardTable.GetBotCardCount();

        bool shouldDraw = EvaluateNextMove(botScore, cardCount);

        if (shouldDraw)
        {
            cardTable.BotDrawCard();
            return true; // Продолжаем сессию, будем думать еще раз
        }

        return false;
    }

    protected override void OnBotFinishedSession()
    {
        cardTable.BotStand();
        Debug.Log($"[BlackGreg ИИ] Бот {gameObject.name} завершил ход (Stand).");
        base.OnBotFinishedSession();
        // Здесь можно вызвать логику сравнения счетов на столе, если это не делает сам стол
    }

    private bool EvaluateNextMove(int score, int cardCount)
    {
        if (cardCount < 2) return true;
        if (cardCount >= 10) return false;

        if (Random.value < stupidityChance)
        {
            if (score >= 19) return true;  // Безумная глупость
            if (score <= 11) return false; // Испугался
        }

        int standThreshold = personality switch
        {
            BotPersonality.Cautious => 15,
            BotPersonality.Risky => 18,
            _ => 17
        };

        return score < standThreshold;
    }
}