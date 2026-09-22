using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

public abstract class InteractableBase : NetworkBehaviour, IInteractable
{
    public virtual InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;

    public virtual int Priority => 5;

    public virtual string InteractionPromptText => "Укажите промпт";

    public virtual float HoldDuration => 0;

    public virtual bool CanInteract(GameObject interactor)
    {
        return false;
    }

    public virtual bool HasAnyAvailableInteractor()
    {
        return false;
    }


    public virtual void Interact(GameObject interactor)
    {
    }
}

