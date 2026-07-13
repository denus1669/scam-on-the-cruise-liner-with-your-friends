using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Интерактивный объект "Входная дверь казино".
/// Работает только в фазе Preparation — запускает новый игровой день.
/// </summary>
public class DoorInteractable : NetworkBehaviour, IInteractable
{
    [Header("Настройки взаимодействия")]
    [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
    [SerializeField] private int priority = 10;
    [SerializeField] private string promptText = "Начать день (E)";

    // Кэш для оптимизации обновления текста подсказки
    private string m_CachedPrompt;
    private GameSessionManager.SessionPhase m_LastPhase;

    public InteractionTriggerMode TriggerMode => triggerMode;
    public int Priority => priority;
    public float HoldDuration => 0f; // Мгновенное взаимодействие

    /// <summary>
    /// Динамический текст подсказки. Меняется в зависимости от фазы.
    /// </summary>
    public string InteractionPromptText
    {
        get
        {
            var manager = GameSessionManager.Instance;
            var currentPhase = manager != null ? manager.CurrentPhase : GameSessionManager.SessionPhase.Preparation;

            if (currentPhase != m_LastPhase || m_CachedPrompt == null)
            {
                m_LastPhase = currentPhase;

                m_CachedPrompt = currentPhase switch
                {
                    GameSessionManager.SessionPhase.Preparation => promptText,
                    GameSessionManager.SessionPhase.GamePhase => "Идёт игровой день...",
                    _ => promptText
                };
            }

            return m_CachedPrompt;
        }
    }

    /// <summary>
    /// Взаимодействие доступно только в фазе Preparation.
    /// </summary>
    public bool CanInteract(GameObject interactor)
    {
        var manager = GameSessionManager.Instance;
        if (manager == null) return false;

        if (!interactor.TryGetComponent<NetworkObject>(out var netObj))
            return false;

        // Проверка, что это игрок
        if (!netObj.IsPlayerObject) return false;

        // Только в фазе Preparation
        return manager.CurrentPhase == GameSessionManager.SessionPhase.Preparation;
    }

    /// <summary>
    /// Запускает новый игровой день.
    /// </summary>
    public void Interact(GameObject interactor)
    {
        if (!IsSpawned) return;

        var manager = GameSessionManager.Instance;
        if (manager == null) return;

        // Финальная проверка
        if (manager.CurrentPhase != GameSessionManager.SessionPhase.Preparation)
        {
            Debug.Log("[DoorInteractable] Попытка взаимодействия вне фазы Preparation");
            return;
        }

        Debug.Log("[DoorInteractable] Игрок начинает новый день!");
        manager.StartDayServerRpc();
    }
}