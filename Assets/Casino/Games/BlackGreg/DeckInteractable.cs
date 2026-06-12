using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Интерактивный объект "Колода карт".
/// При взаимодействии дает команду столу выдать карту игроку.
/// </summary>
public class DeckInteractable : NetworkBehaviour, IInteractable
{
    [Header("Связи")]
    [SerializeField] private BlackGregManager blackGregManager;
    [SerializeField] private TableInteractable tableInteractable;

    [Header("Настройки взаимодействия")]
    [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
    [SerializeField] private int priority = 0;
    [SerializeField] private string promptText = "Взять карту (E)";

    public InteractionTriggerMode TriggerMode => triggerMode;
    public int Priority => priority;
    public string InteractionPromptText => promptText;

    public bool CanInteract(GameObject interactor)
    {
        if (tableInteractable == null) return false;
        if (!tableInteractable.IsOccupied()) return false;

        ulong clientId = interactor.GetComponent<NetworkObject>().OwnerClientId;
        return clientId == tableInteractable.GetOccupyingClientId();
    }

    public void Interact(GameObject interactor)
    {
        if (!IsSpawned) return;

        if (blackGregManager != null)
        {
            // Передаём clientId через аргумент RPC
            ulong clientId = interactor.GetComponent<NetworkObject>().OwnerClientId;
            blackGregManager.RequestDrawCardServerRpc(clientId);
        }
        else
        {
            Debug.LogError("DeckInteractable: tableManager is null");
        }
    }
}
