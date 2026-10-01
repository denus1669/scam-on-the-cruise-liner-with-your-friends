using Assets.Casino.PhaseDay;
using Assets.Casino.PhaseDay.GameStatePhase;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PurchaseManager : NetworkBehaviour
{
    public static PurchaseManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameSessionManager gameSessionManager;

    [Header("Phase visibility")]
    [SerializeField] private GameState visibleState = GameState.Preparing;

    [SerializeField] private readonly HashSet<PurchasableObject> _objects = new();

    public static void RegisterObject(PurchasableObject obj)
    {
        if (Instance == null || obj == null) return;
        Instance.Register(obj);
    }

    public static void UnregisterObject(PurchasableObject obj)
    {
        if (Instance == null || obj == null) return;
        Instance.Unregister(obj);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (Instance != this) return;

        base.OnNetworkSpawn();

        if (gameSessionManager == null)
            gameSessionManager = GameSessionManager.Instance;

        if (gameSessionManager != null)
            gameSessionManager.OnStateChanged += HandleGameStateChanged;
        else
            GameSessionManager.OnInstanceReady += HandleGameSessionReady;

        CollectSceneObjects();
        ApplyCurrentPhase();
    }

    public override void OnNetworkDespawn()
    {
        if (Instance != this)
        {
            base.OnNetworkDespawn();
            return;
        }

        if (gameSessionManager != null)
            gameSessionManager.OnStateChanged -= HandleGameStateChanged;

        GameSessionManager.OnInstanceReady -= HandleGameSessionReady;

        base.OnNetworkDespawn();
    }

    private void HandleGameSessionReady()
    {
        if (gameSessionManager == null)
            gameSessionManager = GameSessionManager.Instance;

        if (gameSessionManager == null) return;

        gameSessionManager.OnStateChanged += HandleGameStateChanged;
        ApplyCurrentPhase();
    }

    private void CollectSceneObjects()
    {
        var objects = FindObjectsByType<PurchasableObject>(FindObjectsSortMode.None);

        foreach (var obj in objects)
            Register(obj);
    }

    private void Register(PurchasableObject obj)
    {
        if (obj == null) return;

        _objects.Add(obj);
        ApplyPhaseToObject(obj);
    }

    private void Unregister(PurchasableObject obj)
    {
        if (obj == null) return;
        _objects.Remove(obj);
    }

    private void HandleGameStateChanged(GameState newState)
    {
        ApplyPhase(newState);
    }

    private void ApplyCurrentPhase()
    {
        if (gameSessionManager == null) return;
        ApplyPhase(gameSessionManager.CurrentState);
    }

    private void ApplyPhase(GameState state)
    {
        bool visible = state == visibleState;

        _objects.RemoveWhere(x => x == null);

        foreach (var obj in _objects)
            obj.SetPhaseVisible(visible);
    }

    private void ApplyPhaseToObject(PurchasableObject obj)
    {
        if (obj == null) return;
        if (gameSessionManager == null) return;

        obj.SetPhaseVisible(gameSessionManager.CurrentState == visibleState);
    }
}