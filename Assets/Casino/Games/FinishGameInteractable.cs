using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Интерактивный объект "Кнопка завершения игры".
/// Показывает статус через UI-подсказку и отправляет запрос на сервер при удержании.
/// </summary>
public class FinishGameInteractable : NetworkBehaviour, IInteractable
{
    [Header("Стол")]
    [SerializeField] private BlackGregTable blackGregTable;

    [Header("Настройки взаимодействия")]
    [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
    [SerializeField] private int priority = 5;
    [SerializeField] private string promptText = "Завершить игру (Удерживайте E)";

    // Кэш для оптимизации обновления текста подсказки
    private string m_CachedPrompt;
    private bool m_LastGameStarted;
    private bool m_LastCanFinish;

    public InteractionTriggerMode TriggerMode => triggerMode;
    public int Priority => priority;
    public float HoldDuration => 2f;

    /// <summary>
    /// Динамический текст подсказки. Обновляется только при смене состояния стола.
    /// </summary>
    public string InteractionPromptText
    {
        get
        {
            bool gameStarted = blackGregTable != null && blackGregTable.IsGameStarted;
            bool canFinish = blackGregTable != null && blackGregTable.CanPlayerFinish();

            if (gameStarted != m_LastGameStarted || canFinish != m_LastCanFinish || m_CachedPrompt == null)
            {
                m_LastGameStarted = gameStarted;
                m_LastCanFinish = canFinish;

                m_CachedPrompt = blackGregTable == null ? promptText :
                    !gameStarted ? "Игра не началась" :
                    !canFinish ? "Условия не выполнены" :
                    promptText;
            }

            return m_CachedPrompt;
        }
    }

    /// <summary>
    /// Определяет, может ли объект быть в фокусе.
    /// Возвращает true, если игрок является владельцем стола и игра началась.
    /// НЕ проверяет CanPlayerFinish, чтобы подсказка отображалась всегда.
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
    /// Выполняется после успешного удержания кнопки.
    /// Содержит финальную проверку условий перед отправкой RPC.
    /// </summary>
    public void Interact(GameObject interactor)
    {
        if (!IsSpawned || blackGregTable == null)
            return;

        // Финальная проверка на клиенте перед отправкой запроса
        if (!blackGregTable.CanPlayerFinish())
        {
            Debug.Log("[FinishGameInteractable] Удержание завершено, но условия больше не выполнены.");
            return;
        }

        ulong clientId = interactor.GetComponent<NetworkObject>().OwnerClientId;
        blackGregTable.RequestFinishGameServerRpc(clientId);
    }
}