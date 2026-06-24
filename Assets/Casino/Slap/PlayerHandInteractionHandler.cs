using Unity.Netcode;
using UnityEngine;
using Blocks.Gameplay.Core;

/// <summary>
/// Обрабатывает удары по рукам для реального ИГРОКА.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PlayerHandInteractionHandler : NetworkBehaviour, IHandInteractable
{
    public void ExecuteHandInteraction(ulong interactorClientId)
    {
        if (!IsServer)
        {
            RequestInteractionServerRpc(interactorClientId);
            return;
        }

        ResolveInteraction(interactorClientId);
    }

    [Rpc(SendTo.Server)]
    private void RequestInteractionServerRpc(ulong interactorClientId)
    {
        ResolveInteraction(interactorClientId);
    }

    private void ResolveInteraction(ulong interactorClientId)
    {
        HandlePlayerInteraction(interactorClientId);
    }

    #region Заготовки для будущих эффектов (Stubs)

    protected virtual void HandlePlayerInteraction(ulong interactorClientId)
    {
        ulong targetPlayerId = OwnerClientId;
        Debug.Log($"[ЗАГЛУШКА] Игрок {interactorClientId} ударил по рукам другого ИГРОКА {targetPlayerId}!");

        // TODO: Логика PvP взаимодействия (например, если игрок пытался украсть фишки, или просто хулиганство)
    }

    #endregion
}