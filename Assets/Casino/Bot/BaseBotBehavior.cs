using System;
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
    protected BotDispleasureController displeasureController;

    protected bool isPlaying = false;

    // --- ГЛОБАЛЬНЫЕ СОБЫТИЯ ДЛЯ ВИЗУАЛА ---
    public event Action<string> OnBluffStarted;

    protected virtual void Awake()
    {
        botAgent = GetComponent<BotAgent>();
        cheatController = GetComponent<CheatController>();
        displeasureController = GetComponent<BotDispleasureController>();

        // Убрали botAnimator!
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

    protected virtual IEnumerator PlaySessionRoutine()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(delayBetweenActionsMin, delayBetweenActionsMax));

        while (isPlaying && gameTable.IsGameStarted)
        {
            yield return StartCoroutine(SuspiciousActionPhase());

            bool shouldContinueSession = EvaluateAndPerformGameAction();

            if (!shouldContinueSession)
            {
                OnBotFinishedSession();
                yield break;
            }

            yield return new WaitForSeconds(UnityEngine.Random.Range(delayBetweenActionsMin, delayBetweenActionsMax));
        }
    }

    protected virtual IEnumerator SuspiciousActionPhase()
    {
        if (cheatController == null) yield break;

        float currentCheatChance = baseCheatChance * GetPersonalityCheatModifier();
        float currentBluffChance = baseBluffChance * GetPersonalityBluffModifier();

        bool isBeingWatched = CheckIfPlayerIsWatching();
        if (isBeingWatched) currentCheatChance *= watchedCheatMultiplier;

        float randomRoll = UnityEngine.Random.value;

        if (randomRoll < currentCheatChance)
        {
            if (cheatController.TryInitiateCheat(gameTable))
            {
                while (cheatController.IsCheating) yield return null;
            }
        }
        else if (randomRoll < currentCheatChance + currentBluffChance)
        {
            yield return StartCoroutine(PlayBluffRoutine());
        }

        yield break;
    }

    private IEnumerator PlayBluffRoutine()
    {
        if (bluffAnimationTriggers.Length == 0) yield break;

        _isBluffing.Value = true;
        string randomBluff = bluffAnimationTriggers[UnityEngine.Random.Range(0, bluffAnimationTriggers.Length)];

        // Вместо аниматора - шлем сигнал всем клиентам
        NotifyBluffStartedClientRpc(randomBluff);

        yield return new WaitForSeconds(2.0f);
        _isBluffing.Value = false;
    }

    [ClientRpc]
    private void NotifyBluffStartedClientRpc(string triggerName)
    {
        OnBluffStarted?.Invoke(triggerName);
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

    protected abstract bool EvaluateAndPerformGameAction();
    protected abstract void OnBotFinishedSession();
    protected virtual float GetPersonalityCheatModifier() => personality == BotPersonality.Risky ? 1.5f : (personality == BotPersonality.Cautious ? 0.5f : 1f);
    protected virtual float GetPersonalityBluffModifier() => personality == BotPersonality.Risky ? 0.8f : (personality == BotPersonality.Cautious ? 1.2f : 1f);
    protected virtual bool CheckIfPlayerIsWatching() => false;
    public void SetPersonality(BotPersonality newPersonality) => personality = newPersonality;

    #endregion
}