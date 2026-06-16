using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Интерактивный объект "Колода карт".
/// При взаимодействии даёт команду столу выдать карту игроку.
/// </summary>
public class DeckInteractable : NetworkBehaviour, IInteractable
{
    [Header("Стол")]
    [SerializeField] private BlackGregTable blackjackTable;

    [Header("Настройки взаимодействия")]
    [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
    [SerializeField] private int priority = 0;
    [SerializeField] private string promptText = "Взять карту (E)";

    public InteractionTriggerMode TriggerMode => triggerMode;
    public int Priority => priority;
    public string InteractionPromptText => promptText;

    public bool CanInteract(GameObject interactor)
    {
        if (blackjackTable == null || !blackjackTable.IsOccupied)
            return false;

        ulong clientId = interactor.GetComponent<NetworkObject>().OwnerClientId;
        return clientId == blackjackTable.OccupiedByClientId;
    }

    public void Interact(GameObject interactor)
    {
        if (!IsSpawned || blackjackTable == null)
            return;

        ulong clientId = interactor.GetComponent<NetworkObject>().OwnerClientId;
        blackjackTable.RequestDrawCardServerRpc(clientId);
    }
}