using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Collider))]
public class TeleportTrigger : NetworkBehaviour
{
    [SerializeField] private Transform _destination;
    [SerializeField] private bool _isActiveSphere;
    // Серверный кулдаун для каждого игрока отдельно
    [SerializeField] private float SERVER_COOLDOWN_TELEPORT = 10f;
    
    private System.Collections.Generic.Dictionary<ulong, float> _serverCooldowns = new();


    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        var teleporter = other.GetComponent<PlayerTeleporter>();
        if (teleporter == null) return;

        ulong clientId = teleporter.OwnerClientId;

        // Проверяем серверный кулдаун
        if (_serverCooldowns.TryGetValue(clientId, out float lastTime))
        {
            if (Time.time - lastTime < SERVER_COOLDOWN_TELEPORT) return;
        }
        _serverCooldowns[clientId] = Time.time;

        teleporter.TeleportClientRpc(_destination.position, _isActiveSphere);
    }
}