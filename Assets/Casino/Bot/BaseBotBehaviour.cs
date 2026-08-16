using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Базовый класс для ИИ бота. Содержит универсальную логику: 
/// жизненный цикл сессии, мухлеж, блеф и реакции на игрока.
/// </summary>
public abstract class BaseBotBehaviour : NetworkBehaviour, IBotGameBehaviour
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

    // Ссылки на универсальные компоненты бота
    protected BotAgent botAgent;
    protected IGameTable gameTable;
    protected CheatController cheatController;
    protected BotBluffController bluffController;          
    protected BotDispleasureController displeasureController;

    private readonly NetworkVariable<bool> _hasBotStood = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public bool HasBotStood => _hasBotStood.Value;

    public event Action<bool> OnBotStoodChanged;

    protected bool isPlaying = false;

    /// <summary>
    /// Тип стола, с которым работает это поведение.
    /// </summary>
    public abstract System.Type SupportedTableType { get; }

    protected virtual void Awake()
    {
        botAgent = GetComponent<BotAgent>();
        cheatController = GetComponent<CheatController>();
        bluffController = GetComponent<BotBluffController>(); 
        displeasureController = GetComponent<BotDispleasureController>();
    }

    public virtual void InitializeGame(IGameTable table)
    {
        // Отписка от предыдущего стола (защита от двойной подписки)
        if (gameTable != null && IsServer)
        {
            gameTable.OnGameStarted -= OnGameStarted;
            gameTable.OnGameEnded -= OnGameEnded;
        }

        gameTable = table;

        if (IsServer)
        {
            _hasBotStood.Value = false; // Сброс
            NotifyBotStoodChangedClientRpc(false);


            gameTable.OnGameStarted += OnGameStarted;
            gameTable.OnGameEnded += OnGameEnded;
        }
    }
    
    protected virtual void OnEnable()
    {
        if (!IsServer) return;

    }

    protected virtual void OnDisable()
    {
        if (isPlaying) EndSession();

        if (gameTable != null && IsServer)
        {
            gameTable.OnGameStarted -= OnGameStarted;
            gameTable.OnGameEnded -= OnGameEnded;
            gameTable = null;
        }
    }

    public virtual void HandleBotArrivedChanged(bool hasArrived)
    {
    }

    public virtual void StartSession()
    {
        Debug.Log($"[BaseBotBehavior] Бот начал сессию на столе '{gameTable.TableName}'");
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
        Debug.Log("ONBOTFIFNFFSDSDS");
        EndSession();
        _hasBotStood.Value = false;
        NotifyBotStoodChangedClientRpc(false);
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
            // МУХЛЁЖ
            if (cheatController.TryInitiateCheat(gameTable))
            {
                while (cheatController.IsCheating) yield return null;
            }
        }
        else if (randomRoll < currentCheatChance + currentBluffChance)
        {
            // БЛЕФ — делегируем в контроллер
            if (bluffController != null && bluffController.TryBluff())
            {
                // Ждём окончания блефа (аналогично мухлежу)
                while (bluffController.IsBluffing) yield return null;
            }
        }

        yield break;
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
    protected virtual void OnBotFinishedSession()
    {
        if (!IsServer) return;
        _hasBotStood.Value = true;
        NotifyBotStoodChangedClientRpc(true);
    }

    [ClientRpc]
    private void NotifyBotStoodChangedClientRpc(bool hasStood)
    {
        OnBotStoodChanged?.Invoke(hasStood);
    }
    protected abstract bool EvaluateAndPerformGameAction();
    protected virtual float GetPersonalityCheatModifier() => personality == BotPersonality.Risky ? 1.5f : (personality == BotPersonality.Cautious ? 0.5f : 1f);
    protected virtual float GetPersonalityBluffModifier() => personality == BotPersonality.Risky ? 0.8f : (personality == BotPersonality.Cautious ? 1.2f : 1f);
    protected virtual bool CheckIfPlayerIsWatching() => false;
    public void SetPersonality(BotPersonality newPersonality) => personality = newPersonality;

    #endregion
}