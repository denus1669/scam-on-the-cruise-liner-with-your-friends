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

    [Header("Настройки поведения (Мухлеж и Блеф)")]
    [Tooltip("Базовый шанс смухлевать перед взятием карты")]
    [SerializeField] private float baseCheatChance = 0.15f;
    [Tooltip("Базовый шанс проиграть фейковую анимацию (блеф), чтобы запутать игрока")]
    [SerializeField] private float baseBluffChance = 0.25f;
    [Tooltip("Насколько сильно падает шанс мухлежа, если игрок смотрит на бота")]
    [SerializeField] private float watchedCheatMultiplier = 0.1f;

    // Список фейковых анимаций (триггеров), которые просто пугают игрока
    [SerializeField] private string[] bluffAnimationTriggers = { "Bluff_NoseScratch", "Bluff_FixCards", "Bluff_Cough" };

    private BotAgent botAgent;
    private IGameTable gameTable;
    private ICardGameTable cardTable;
    private CheatController cheatController;
    private Animator botAnimator;

    private bool isPlaying = false;

    private void Awake()
    {
        botAgent = GetComponent<BotAgent>();
        cheatController = GetComponent<CheatController>();
        botAnimator = GetComponentInChildren<Animator>();
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
        if (botAgent != null) botAgent.GoToExit();
    }

    // === ИГРОВАЯ ЛОГИКА ===

    private IEnumerator PlaySessionRoutine()
    {
        isPlaying = true;
        Debug.Log($"[BlackGreg ИИ] Бот {gameObject.name} начинает игровую сессию.");

        yield return new WaitForSeconds(Random.Range(delayBetweenActionsMin, delayBetweenActionsMax));

        // Получаем контроллер мухлежа с бота
        CheatController cheatController = GetComponent<CheatController>();

        while (isPlaying)
        {
            // Если игра уже не идёт — выходим
            if (!gameTable.IsGameStarted)
            {
                Debug.Log($"[BlackGreg ИИ] Игра завершена, бот прекращает сессию.");
                isPlaying = false;
                yield break;
            }

            // 1. ФАЗА ПОДОЗРИТЕЛЬНЫХ ДЕЙСТВИЙ (Блеф или Мухлеж)
            yield return StartCoroutine(SuspiciousActionPhase());

            // 2. ФАЗА ПРИНЯТИЯ ИГРОВОГО РЕШЕНИЯ
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

    /// <summary>
    /// Фаза, в которой бот решает: сидеть спокойно, пустить пыль в глаза (блеф) или попытаться смухлевать.
    /// </summary>
    private IEnumerator SuspiciousActionPhase()
    {
        if (cheatController == null) yield break;

        // Модифицируем шансы от личности
        float persCheatModifier = personality == BotPersonality.Risky ? 1.5f : (personality == BotPersonality.Cautious ? 0.5f : 1f);
        float persBluffModifier = personality == BotPersonality.Risky ? 0.8f : (personality == BotPersonality.Cautious ? 1.2f : 1f);

        float currentCheatChance = baseCheatChance * persCheatModifier;
        float currentBluffChance = baseBluffChance * persBluffModifier;

        // Если на нас смотрят — сильно режем шанс РЕАЛЬНОГО мухлежа
        bool isBeingWatched = CheckIfPlayerIsWatching();
        if (isBeingWatched)
        {
            currentCheatChance *= watchedCheatMultiplier;
        }

        float randomRoll = Random.value; // от 0.0 до 1.0

        if (randomRoll < currentCheatChance)
        {
            // ПЫТАЕМСЯ СМУХЛЕВАТЬ
            bool isCheatingStarted = cheatController.TryInitiateCheat(gameTable);
            if (isCheatingStarted)
            {
                // Ждем окончания процесса мухлежа
                while (cheatController.IsCheating)
                {
                    yield return null;
                }
            }
        }
        else if (randomRoll < currentCheatChance + currentBluffChance)
        {
            // БЛЕФУЕМ (Проигрываем случайную пустую анимацию)
            if (bluffAnimationTriggers.Length > 0)
            {
                string randomBluff = bluffAnimationTriggers[Random.Range(0, bluffAnimationTriggers.Length)];
                PlayBluffAnimationClientRpc(randomBluff);

                // Ждем пару секунд, пока пройдет анимация блефа, 
                // давая игроку возможность ошибиться и нажать "E".
                yield return new WaitForSeconds(2.0f);
            }
        }
        // Иначе ничего не делаем, идем дальше.
    }

    /// <summary>
    /// Серверная проверка: смотрит ли игрок на бота.
    /// </summary>
    private bool CheckIfPlayerIsWatching()
    {
        // Проходим по всем подключенным клиентам
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var playerObject = client.PlayerObject;
            if (playerObject != null)
            {
                // Дистанция до игрока
                float distance = Vector3.Distance(transform.position, playerObject.transform.position);

                // Если игрок ближе 5 метров (настраиваемо)
                if (distance < 5.0f)
                {
                    // Проверяем, направлен ли взгляд игрока (его transform.forward) на бота
                    Vector3 directionToBot = (transform.position - playerObject.transform.position).normalized;
                    float dotProduct = Vector3.Dot(playerObject.transform.forward, directionToBot);

                    // Если dotProduct близок к 1, значит игрок смотрит прямо на нас.
                    // 0.7f - это примерно угол конуса зрения в 45 градусов в обе стороны.
                    if (dotProduct > 0.7f)
                    {
                        return true; // Нас спалили, игрок смотрит!
                    }
                }
            }
        }
        return false;
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

    // === RPC ДЛЯ БЛЕФА ===
    [ClientRpc]
    private void PlayBluffAnimationClientRpc(string triggerName)
    {
        Debug.Log($"[BlackGreg ИИ] Бот {gameObject.name} БЛЕФУЕТ (Анимация: {triggerName})");
        if (botAnimator != null)
        {
            botAnimator.SetTrigger(triggerName);
        }
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