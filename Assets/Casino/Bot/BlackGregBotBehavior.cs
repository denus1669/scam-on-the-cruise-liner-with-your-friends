using Blocks.Gameplay.Core;
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

    private BotAgent _botAgent;
    private BlackGregManager blackGregManager;
    private TableInteractable tableInteractable;
    private bool _isPlaying = false;

    private void Awake()
    {
        _botAgent = GetComponent<BotAgent>();
    }

    // === РЕАЛИЗАЦИЯ ИНТЕРФЕЙСА ===

    public void InitializeGame(NetworkBehaviour tableManager)
    {
        if (tableManager is BlackGregManager manager)
        {
            blackGregManager = manager;
            tableInteractable = manager.TableInteractable;

            // Подписываемся на события стола, так как теперь мы знаем, за каким столом играем
            if (IsServer && tableInteractable != null)
            {
                blackGregManager.gameInProgress.OnValueChanged += OnGameStatusChanged;
            }

            Debug.Log($"[BlackGreg ИИ] Бот {gameObject.name} инициализирован для стола {manager.name}");
        }
        else
        {
            Debug.LogError($"[BlackGreg ИИ] Переданный менеджер не является BlackGregManager!");
        }
    }

    public void StartSession()
    {
        if (!IsServer || _isPlaying) return;
        StartCoroutine(PlaySessionRoutine());
    }

    public void EndSession()
    {
        StopAllCoroutines();
        _isPlaying = false;
        Debug.Log($"[BlackGreg ИИ] Бот {gameObject.name} завершил сессию.");
    }

    // === ЛОГИКА ПОВЕДЕНИЯ ===

    /// <summary>
    /// Реагирует на начало и конец игры.
    /// </summary>
    private void OnGameStatusChanged(bool oldState, bool newState)
    {
        if (!IsServer) return;

        if (newState)
        {
            // gameInProgress стал true (игрок взял первую карту в PlayerDrawCard)
            Debug.Log("[BlackGreg ИИ] Игра началась, бот включается.");
            StartSession();
        }
        else
        {
            // gameInProgress стал false (вызван FinishGame)
            Debug.Log("[BlackGreg ИИ] Игра закончилась, бот выключается.");
            EndSession();
        }
    }

    private IEnumerator PlaySessionRoutine()
    {
        _isPlaying = true;
        Debug.Log($"[BlackGreg ИИ] Бот {gameObject.name} начинает игровую сессию.");

        yield return new WaitForSeconds(Random.Range(delayBetweenActionsMin, delayBetweenActionsMax));

        while (_isPlaying)
        {
            // === ПРОВЕРКА СОСТОЯНИЯ ГОНКИ (Race Condition) ===
            if (!blackGregManager.gameInProgress.Value)
            {
                Debug.Log($"[BlackGreg ИИ] Игра завершена, бот прекращает сессию.");
                _isPlaying = false;
                yield break;
            }
            // ================================================

            List<CardData> botCards = blackGregManager.GetBotHandCopy();
            int currentScore = blackGregManager.CalculateHandValue(botCards);
            int cardCount = botCards.Count;

            Debug.Log($"[BlackGreg ИИ] Бот имеет {cardCount} карт, сумма очков: {currentScore}");

            bool shouldDraw = EvaluateNextMove(currentScore, cardCount);

            if (shouldDraw)
            {
                Debug.Log($"[BlackGreg ИИ] Бот решает ВЗЯТЬ карту.");
                blackGregManager.BotDrawCard();
            }
            else
            {
                Debug.Log($"[BlackGreg ИИ] Бот решает ОСТАНОВИТЬСЯ.");
                blackGregManager.BotStand();
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

        int standThreshold = 17;
        switch (personality)
        {
            case BotPersonality.Cautious: standThreshold = 15; break;
            case BotPersonality.Risky: standThreshold = 18; break;
            case BotPersonality.Balanced:
            default: standThreshold = 17; break;
        }

        return score < standThreshold;
    }

    public void SetPersonality(BotPersonality newPersonality)
    {
        this.personality = newPersonality;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer && blackGregManager != null)
        {
            // === ГЛАВНОЕ ИЗМЕНЕНИЕ ===
            // Подписываемся на статус игры, а не на занятость стола!
            blackGregManager.gameInProgress.OnValueChanged += OnGameStatusChanged;
        }
    }

    // Отписываемся от событий при уничтожении
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (tableInteractable != null)
        {
            blackGregManager.gameInProgress.OnValueChanged -= OnGameStatusChanged;
        }
    }
}