using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Интерактивный объект для завершения игры.
/// Двухступенчатая система:
/// 1. Удержание 2 сек → вскрыть карты (RevealHands)
/// 2. Мгновенно → завершить партию (FinishGame)
/// </summary>
public class FinishGameInteractable : NetworkBehaviour, IInteractable
{
    [Header("Стол")]
    [SerializeField] private BlackGregTable blackGregTable;

    [Header("Настройки взаимодействия")]
    [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
    [SerializeField] private int priority = 5;

    // Кэш для оптимизации обновления текста подсказки
    private string m_CachedPrompt;
    private bool m_LastGameStarted;
    private bool m_LastCanReveal;
    private bool m_LastCanFinish;

    public InteractionTriggerMode TriggerMode => triggerMode;
    public int Priority => priority;

    /// <summary>
    /// Динамическое время удержания: 2 сек для Reveal, 0 сек для Finish.
    /// </summary>
    public float HoldDuration => blackGregTable != null && blackGregTable.IsRevealed ? 0f : 2f;

    /// <summary>
    /// Динамический текст подсказки.
    /// </summary>
    public string InteractionPromptText
    {
        get
        {
            bool gameStarted = blackGregTable != null && blackGregTable.IsGameStarted;
            bool canReveal = blackGregTable != null && blackGregTable.CanPlayerReveal();
            bool canFinish = blackGregTable != null && blackGregTable.CanPlayerFinish();

            if (gameStarted != m_LastGameStarted || canReveal != m_LastCanReveal ||
                canFinish != m_LastCanFinish || m_CachedPrompt == null)
            {
                m_LastGameStarted = gameStarted;
                m_LastCanReveal = canReveal;
                m_LastCanFinish = canFinish;

                m_CachedPrompt = blackGregTable == null ? "Стол недоступен" :
                    !gameStarted ? "Игра не началась" :
                    canReveal ? "Вскрыть карты (Удерживайте E)" :
                    canFinish ? "Завершить партию (E)" :
                    "Бот не закончил ходить или Игрок не взял 2 карты";
            }

            return m_CachedPrompt;
        }
    }

    /// <summary>
    /// Определяет, может ли объект быть в фокусе.
    /// </summary>
    public bool CanInteract(GameObject interactor)
    {
        if (blackGregTable == null || !blackGregTable.IsOccupied || !blackGregTable.IsGameStarted)
            return false;

        if (!interactor.TryGetComponent<NetworkObject>(out var netObj))
            return false;

        // Только владелец стола может взаимодействовать
        return netObj.OwnerClientId == blackGregTable.OccupiedByClientId;
    }

    /// <summary>
    /// Выполняется после успешного удержания/нажатия кнопки.
    /// </summary>
    public void Interact(GameObject interactor)
    {
        if (!IsSpawned || blackGregTable == null)
            return;

        ulong clientId = interactor.GetComponent<NetworkObject>().OwnerClientId;

        if (blackGregTable.CanPlayerReveal())
        {
            blackGregTable.RevealHandsServerRpc(clientId);
        }
        else if (blackGregTable.CanPlayerFinish())
        {
            blackGregTable.FinishGameServerRpc(clientId);
        }
    }
}