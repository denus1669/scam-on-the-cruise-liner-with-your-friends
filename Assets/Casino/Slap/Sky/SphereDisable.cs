using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(Collider))]
public class SphereDisableTrigge : NetworkBehaviour
{
    [SerializeField] private bool _isActiveSphere = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        var teleporter = other.GetComponent<PlayerTeleporter>();
        if (teleporter == null) return;

        teleporter.SphereActiveClientRpc(_isActiveSphere);
    }
}