using Blocks.Gameplay.Core;
using System;
using Unity.Netcode;
using UnityEngine;

public abstract class InteractableBase : NetworkBehaviour, IInteractable
{
    public virtual event Action<bool> OnAvailabilityChanged;

    public virtual InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;

    public virtual int Priority => 5;

    public virtual string InteractionPromptText => "Укажите промпт";

    public virtual float HoldDuration => 0;

    public virtual bool CanInteract(GameObject interactor)
    {
        return false;
    }

    public virtual void Interact(GameObject interactor)
    {
    }
    public virtual bool HasAnyAvailableInteractor()
    {
        return true;
    }
    [Rpc(SendTo.Server)]
    public virtual void HasAnyAvailableInteractorServerRpc()
    {
        HasAnyAvailableInteractorClientRpc();
    }

    [ClientRpc]
    public virtual void HasAnyAvailableInteractorClientRpc()
    {

    }
}

