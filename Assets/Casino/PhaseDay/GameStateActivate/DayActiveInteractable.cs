using Blocks.Gameplay.Core;
using System.Net;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Интерактивный объект "Входная дверь казино".
/// Работает только в фазе Preparation — запускает новый игровой день.
/// </summary>
public class DayActiveInteractable : NetworkBehaviour, IInteractable
{
    [Header("Настройки взаимодействия")]
    [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
    [SerializeField] private int priority = 10;
    [SerializeField] private string promptText = "Начать день (E)";

    [SerializeField] private GameObject _firstDoorPart;
    [SerializeField] private GameObject _doorOpenPart;
    [SerializeField] private float _pivotPoint; 

    // Кэш для оптимизации обновления текста подсказки
    private string m_CachedPrompt;
    private GameState m_LastState;

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
            var currentPhase = manager != null ? manager.CurrentState : GameState.Preparing;

            if (currentPhase != m_LastState || m_CachedPrompt == null)
            {
                m_LastState = currentPhase;

                m_CachedPrompt = currentPhase switch
                {
                    GameState.Preparing => promptText,
                    GameState.DayActive => "Идёт игровой день...",
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
        return manager.CurrentState == GameState.Preparing;
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
        if (manager.CurrentState != GameState.Preparing)
        {
            Debug.Log("[DayActiveInteractable] Попытка взаимодействия вне фазы Preparation");
            return;
        }

        Debug.Log("[DayActiveInteractable] Игрок начинает новый день!");
        manager.StartDayServerRpc();
        if (_firstDoorPart != null) _firstDoorPart.SetActive(false);
        if (_doorOpenPart != null) OpenDoor();
            
    }

    private void OpenDoor()
    {
        _doorOpenPart.transform.localEulerAngles = new Vector3(0, _pivotPoint, 0);
    }
}