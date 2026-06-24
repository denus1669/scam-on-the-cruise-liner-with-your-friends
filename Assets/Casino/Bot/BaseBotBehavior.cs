using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Базовый класс для ИИ бота. Содержит универсальную логику: 
/// жизненный цикл сессии, мухлеж, блеф и реакции на игрока.
/// </summary>
public abstract class BaseBotBehavior : NetworkBehaviour, IBotGameBehavior
{
    [Header("Универсальные настройки ИИ")]
    [SerializeField] protected BotPersonality personality = BotPersonality.Balanced;

    [Header("Тайминги")]
    [SerializeField] protected float delayBetweenActionsMin = 1.5f;
    [SerializeField] protected float delayBetweenActionsMax = 3.0f;

    [Header("Настройки поведения (Мухлеж и Блеф)")]
    [SerializeField] protected float baseCheatChance = 0.15f;
    [SerializeField] protected float baseBluffChance = 0.25f;
    [SerializeField] protected float watchedCheatMultiplier = 0.1f;
    [SerializeField] protected string[] bluffAnimationTriggers = { "Bluff_Generic" };

    // Сетевое состояние блефа (Мухлеж уже хранится в CheatController)
    protected readonly NetworkVariable<bool> _isBluffing = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    public bool IsBotBluffingActive => _isBluffing.Value;

    // Ссылки на универсальные компоненты бота
    protected BotAgent botAgent;
    protected IGameTable gameTable;
    protected CheatController cheatController;
    protected Animator botAnimator;
    protected BotDispleasureController displeasureController;

    protected bool isPlaying = false;

    protected virtual void Awake()
    {
        botAgent = GetComponent<BotAgent>();
        cheatController = GetComponent<CheatController>();
        botAnimator = GetComponentInChildren<Animator>();
        displeasureController = GetComponent<BotDispleasureController>();
    }

    public virtual void InitializeGame(IGameTable table)
    {
        gameTable = table;

        if (IsServer)
        {
            gameTable.OnGameStarted += OnGameStarted;
            gameTable.OnGameEnded += OnGameEnded;
        }
    }

    public virtual void StartSession()
    {
        if (!IsServer || isPlaying) return;
        isPlaying = true;
        StartCoroutine(PlaySessionRoutine());
    }

    public virtual void EndSession()
    {
        StopAllCoroutines();
        isPlaying = false;
    }

    private void OnGameStarted() => StartSession();

    private void OnGameEnded()
    {
        EndSession();
        if (botAgent != null) botAgent.GoToExit();
    }

    /// <summary>
    /// Главный цикл игры. Чередуется между фазой подозрительных действий и фазой игрового решения.
    /// </summary>
    protected virtual IEnumerator PlaySessionRoutine()
    {
        yield return new WaitForSeconds(Random.Range(delayBetweenActionsMin, delayBetweenActionsMax));

        while (isPlaying && gameTable.IsGameStarted)
        {
            // 1. Универсальная фаза (Мухлеж / Блеф / Ожидание)
            yield return StartCoroutine(SuspiciousActionPhase());

            // 2. Специфичная для игры фаза (Взять карту / Бросить кости / Сделать ставку)
            bool shouldContinueSession = EvaluateAndPerformGameAction();

            if (!shouldContinueSession)
            {
                OnBotFinishedSession();
                yield break;
            }

            yield return new WaitForSeconds(Random.Range(delayBetweenActionsMin, delayBetweenActionsMax));
        }
    }

    /// <summary>
    /// Фаза, в которой бот решает: сидеть спокойно, блефовать или попытаться смухлевать.
    /// </summary>
    protected virtual IEnumerator SuspiciousActionPhase()
    {
        if (cheatController == null) yield break;

        float currentCheatChance = baseCheatChance * GetPersonalityCheatModifier();
        float currentBluffChance = baseBluffChance * GetPersonalityBluffModifier();

        // В будущем здесь будет запрос к BotDispleasureController
        bool isBeingWatched = CheckIfPlayerIsWatching();
        if (isBeingWatched) currentCheatChance *= watchedCheatMultiplier;

        float randomRoll = Random.value;

        if (randomRoll < currentCheatChance)
        {
            // ПЫТАЕМСЯ СМУХЛЕВАТЬ
            if (cheatController.TryInitiateCheat(gameTable))
            {
                while (cheatController.IsCheating) yield return null;
            }
        }
        else if (randomRoll < currentCheatChance + currentBluffChance)
        {
            // БЛЕФУЕМ
            yield return StartCoroutine(PlayBluffRoutine());
        }

        yield break;
    }

    private IEnumerator PlayBluffRoutine()
    {
        if (bluffAnimationTriggers.Length == 0) yield break;

        _isBluffing.Value = true;
        string randomBluff = bluffAnimationTriggers[Random.Range(0, bluffAnimationTriggers.Length)];
        PlayBluffAnimationClientRpc(randomBluff);

        yield return new WaitForSeconds(2.0f);
        _isBluffing.Value = false;
    }

    [ClientRpc]
    private void PlayBluffAnimationClientRpc(string triggerName)
    {
        if (botAnimator != null) botAnimator.SetTrigger(triggerName);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (gameTable != null && IsServer)
        {
            gameTable.OnGameStarted -= OnGameStarted;
            gameTable.OnGameEnded -= OnGameEnded;
        }
    }

    #region Abstract & Virtual Methods (Для наследников)

    /// <summary>
    /// Главный метод для наследников. Здесь бот принимает решение по правилам конкретной игры.
    /// </summary>
    /// <returns>true, если бот продолжает игру (например, взял карту). false, если закончил ход (например, сказал "Стенд").</returns>
    protected abstract bool EvaluateAndPerformGameAction();

    /// <summary>
    /// Вызывается, когда бот принял решение завершить свое участие в текущей сессии (например, встал из-за стола).
    /// </summary>
    protected abstract void OnBotFinishedSession();

    protected virtual float GetPersonalityCheatModifier() => personality == BotPersonality.Risky ? 1.5f : (personality == BotPersonality.Cautious ? 0.5f : 1f);
    protected virtual float GetPersonalityBluffModifier() => personality == BotPersonality.Risky ? 0.8f : (personality == BotPersonality.Cautious ? 1.2f : 1f);

    // Временная заглушка, пока не интегрируем BotDispleasureController
    protected virtual bool CheckIfPlayerIsWatching() => false;
    public void SetPersonality(BotPersonality newPersonality) => personality = newPersonality;


    #endregion
}