using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(BotAgent))]
public class BlackGregBotBehavior : NetworkBehaviour, IBotGameBehavior
{
    [Header("Настройки личности ИИ")]
    [SerializeField] private BotPersonality personality = BotPersonality.Balanced;
    [Range(0f, 1f)]
    [SerializeField] private float stupidityChance = 0.08f;

    [Header("Тайминги")]
    [SerializeField] private float delayBetweenActionsMin = 1.5f;
    [SerializeField] private float delayBetweenActionsMax = 3.0f;

    private BotAgent botAgent;
    private IGameTable gameTable;
    private ICardGameTable cardTable;
    private bool isPlaying = false;

    private void Awake()
    {
        botAgent = GetComponent<BotAgent>();
    }

    // === РЕАЛИЗАЦИЯ IBotGameBehavior ===

    public void InitializeGame(IGameTable table)
    {
        gameTable = table;
        cardTable = table as ICardGameTable;

        if (cardTable == null)
        {
            Debug.LogError($"[BlackGreg ИИ] Стол {table} не реализует ICardGameTable. Бот не может играть.");
            return;
        }

        // Подписываемся на события стола (сервер)
        if (IsServer)
        {
            gameTable.OnGameStarted += OnGameStarted;
            gameTable.OnGameEnded += OnGameEnded;
        }

        Debug.Log($"[BlackGreg ИИ] Бот {gameObject.name} инициализирован для стола {(table as MonoBehaviour)?.name}");
    }

    public void StartSession()
    {
        if (!IsServer || isPlaying) return;
        StartCoroutine(PlaySessionRoutine());
    }

    public void EndSession()
    {
        StopAllCoroutines();
        isPlaying = false;
        Debug.Log($"[BlackGreg ИИ] Бот {gameObject.name} завершил сессию.");
    }

    // === РЕАКЦИЯ НА СОБЫТИЯ СТОЛА ===

    private void OnGameStarted()
    {
        if (!IsServer) return;
        Debug.Log("[BlackGreg ИИ] Игра началась, бот включается.");
        StartSession();
    }

    private void OnGameEnded()
    {
        if (!IsServer) return;
        Debug.Log("[BlackGreg ИИ] Игра закончилась, бот выключается.");
        EndSession();
        // Сообщаем BotAgent, что можно уходить
        if (botAgent != null)
            botAgent.GoToExit();
    }

    // === ИГРОВАЯ ЛОГИКА ===

    private IEnumerator PlaySessionRoutine()
    {
        isPlaying = true;
        Debug.Log($"[BlackGreg ИИ] Бот {gameObject.name} начинает игровую сессию.");

        yield return new WaitForSeconds(Random.Range(delayBetweenActionsMin, delayBetweenActionsMax));

        while (isPlaying)
        {
            // Если игра уже не идёт — выходим
            if (!gameTable.IsGameStarted)
            {
                Debug.Log($"[BlackGreg ИИ] Игра завершена, бот прекращает сессию.");
                isPlaying = false;
                yield break;
            }

            int botScore = cardTable.GetBotScore();
            int cardCount = cardTable.GetBotCardCount();

            Debug.Log($"[BlackGreg ИИ] Бот имеет {cardCount} карт, сумма очков: {botScore}");

            bool shouldDraw = EvaluateNextMove(botScore, cardCount);

            if (shouldDraw)
            {
                Debug.Log("[BlackGreg ИИ] Бот решает ВЗЯТЬ карту.");
                cardTable.BotDrawCard();
            }
            else
            {
                Debug.Log("[BlackGreg ИИ] Бот решает ОСТАНОВИТЬСЯ.");
                cardTable.BotStand();
                yield break;
            }

            yield return new WaitForSeconds(Random.Range(delayBetweenActionsMin, delayBetweenActionsMax));
        }
    }

    private bool EvaluateNextMove(int score, int cardCount)
    {
        if (cardCount < 2) return true;
        if (cardCount >= 10) return false;

        if (Random.value < stupidityChance)
        {
            if (score >= 19)
            {
                Debug.LogWarning("[BlackGreg ИИ] Бот совершает безумную глупость!");
                return true;
            }
            if (score <= 11)
            {
                Debug.LogWarning("[BlackGreg ИИ] Бот испугался и спасовал!");
                return false;
            }
        }

        int standThreshold = personality switch
        {
            BotPersonality.Cautious => 15,
            BotPersonality.Risky => 18,
            _ => 17
        };

        return score < standThreshold;
    }

    public void SetPersonality(BotPersonality newPersonality)
    {
        personality = newPersonality;
    }

    // === СЕТЕВЫЕ МЕТОДЫ ===

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (gameTable != null)
        {
            gameTable.OnGameStarted -= OnGameStarted;
            gameTable.OnGameEnded -= OnGameEnded;
        }
    }
}