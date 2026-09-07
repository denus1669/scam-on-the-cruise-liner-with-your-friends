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
    protected BotCheatController cheatController;
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
        cheatController = GetComponent<BotCheatController>();
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
        if (cheatController == null)
        {
            Debug.LogError($"[SuspiciousAction] Пропуск: cheatController == null ({name})");
            yield break;
        }

        float personalityCheatMod = GetPersonalityCheatModifier();
        float personalityBluffMod = GetPersonalityBluffModifier();

        float currentCheatChance = baseCheatChance * personalityCheatMod;
        float currentBluffChance = baseBluffChance * personalityBluffMod;

        bool isBeingWatched = CheckIfPlayerIsWatching();
        if (isBeingWatched)
        {
            float oldChance = currentCheatChance;
            currentCheatChance *= watchedCheatMultiplier;
            Debug.Log($"[SuspiciousAction] Игрок смотрит! Чит-шанс: {oldChance:F3} -> {currentCheatChance:F3} (x{watchedCheatMultiplier})");
        }

        float randomRoll = UnityEngine.Random.value;
        float totalThreshold = currentCheatChance + currentBluffChance;

        Debug.Log($"[SuspiciousAction] Roll: {randomRoll:F3} | Cheat: {currentCheatChance:F3} | Bluff: {currentBluffChance:F3} | Watched: {isBeingWatched} | Name: {name}");

        if (randomRoll < currentCheatChance)
        {
            Debug.Log($"[SuspiciousAction] >>> ВЫБРАН МУХЛЁЖ (roll {randomRoll:F3} < {currentCheatChance:F3})");

            bool initiated = cheatController.TryInitiateCheat(gameTable);
            Debug.Log($"[SuspiciousAction] TryInitiateCheat результат: {initiated}");

            if (initiated)
            {
                int frames = 0;
                while (cheatController.IsCheating)
                {
                    frames++;
                    yield return null;
                }
                Debug.Log($"[SuspiciousAction] Мухлёж завершён за {frames} кадров");
            }
        }
        else if (randomRoll < totalThreshold)
        {
            Debug.Log($"[SuspiciousAction] >>> ВЫБРАН БЛЕФ (roll {randomRoll:F3} < {totalThreshold:F3})");

            if (bluffController == null)
            {
                Debug.LogWarning("[SuspiciousAction] bluffController == null, блеф пропущен!");
            }
            else
            {
                bool bluffStarted = bluffController.TryBluff();
                Debug.Log($"[SuspiciousAction] TryBluff результат: {bluffStarted}");

                if (bluffStarted)
                {
                    int frames = 0;
                    while (bluffController.IsBluffing)
                    {
                        frames++;
                        yield return null;
                    }
                    Debug.Log($"[SuspiciousAction] Блеф завершён за {frames} кадров");
                }
            }
        }
        else
        {
            Debug.Log($"[SuspiciousAction] >>> НИЧЕГО (roll {randomRoll:F3} >= {totalThreshold:F3})");
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