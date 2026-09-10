using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

public class DayActiveInteractable : NetworkBehaviour, IInteractable
{
    [Header("Настройки взаимодействия")]
    [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
    [SerializeField] private int priority = 10;
    [SerializeField] private string promptText = "Начать день (E)";

    [SerializeField] private GameObject _firstDoorPart;
    [SerializeField] private GameObject _doorOpenPart;
    [SerializeField] private float _pivotPoint;

    private string m_CachedPrompt;
    private GameState m_LastState;

    public InteractionTriggerMode TriggerMode => triggerMode;
    public int Priority => priority;
    public float HoldDuration => 0f;

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

    public bool CanInteract(GameObject interactor)
    {
        var manager = GameSessionManager.Instance;
        if (manager == null) return false;
        if (!interactor.TryGetComponent<NetworkObject>(out var netObj)) return false;
        if (!netObj.IsPlayerObject) return false;
        return manager.CurrentState == GameState.Preparing;
    }

    public void Interact(GameObject interactor)
    {
        if (!IsSpawned) return;

        var manager = GameSessionManager.Instance;
        if (manager == null) return;

        if (manager.CurrentState != GameState.Preparing)
        {
            Debug.Log("[DayActiveInteractable] Попытка взаимодействия вне фазы Preparation");
            return;
        }

        Debug.Log("[DayActiveInteractable] Игрок запрашивает начало дня!");

        // ТОЛЬКО запрос на сервер. Никаких визуальных RPC с клиента!
        manager.StartDayServerRpc();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Подписываемся на событие смены фазы в менеджере
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.OnStateChanged += HandleGameStateChanged;
            // Синхронизируем начальное состояние для поздно подключившихся
            HandleGameStateChanged(GameSessionManager.Instance.CurrentState);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.OnStateChanged -= HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if (newState == GameState.DayActive)
        {
            OpenDoorVisuals();
        }
        else if (newState == GameState.Preparing)
        {
            CloseDoorVisuals();
        }
    }

    private void OpenDoorVisuals()
    {
        if (_firstDoorPart != null) _firstDoorPart.SetActive(false);
        if (_doorOpenPart != null) _doorOpenPart.transform.localEulerAngles = new Vector3(0, _pivotPoint, 0);
    }

    private void CloseDoorVisuals()
    {
        if (_firstDoorPart != null) _firstDoorPart.SetActive(true);
        if (_doorOpenPart != null) _doorOpenPart.transform.localEulerAngles = Vector3.zero;
    }
}