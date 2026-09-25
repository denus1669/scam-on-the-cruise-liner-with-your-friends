using Assets.Casino.Bot;
using Assets.Casino.Games;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class PlayerPlayGameIndicator : NetworkObjectVisibilityIndicator
{
    private NetworkObject _ownerNetworkObject;
    private List<GameTable> _subscribedTables = new List<GameTable>();

    protected override void Awake()
    {
        base.Awake();
        _ownerNetworkObject = GetComponentInParent<NetworkObject>();
        if (_ownerNetworkObject == null)
            Debug.LogWarning($"[PlayerPlayGameIndicator] NetworkObject не найден на {gameObject.name}");
    }

    private void OnEnable()
    {
        var tables = FindObjectsOfType<GameTable>();
        foreach (var table in tables)
        {
            table.OnOccupantChanged += HandleOccupantChanged;
            _subscribedTables.Add(table);
        }
        UpdateIndicatorState("OnEnable");
    }

    private void OnDisable()
    {
        foreach (var table in _subscribedTables)
        {
            if (table != null)
                table.OnOccupantChanged -= HandleOccupantChanged;
        }
        _subscribedTables.Clear();
        HideIndicator();
    }

    private void HandleOccupantChanged(ulong clientId)
    {
        UpdateIndicatorState($"OccupantChanged({clientId})");
    }

    private void UpdateIndicatorState(string reason)
    {
        if (_ownerNetworkObject == null || !_ownerNetworkObject.IsSpawned) return;

        ulong ownerId = _ownerNetworkObject.OwnerClientId;

        bool isPlaying = _subscribedTables.Any(t =>
            t != null && t.IsOccupied && t.OccupiedByClientId == ownerId);

        HandleStateChanged(isPlaying);
    }
}