using Assets.Casino.Bank;
using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PurchasableObject : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CasinoBank casinoBank;
    [SerializeField] private PurchaseVisualController visualController;

    [Header("Settings")]
    [SerializeField] private int price = 100;

    private readonly NetworkVariable<bool> _isPurchased = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool IsPurchased => _isPurchased.Value;
    public int Price => price;

    public event Action<bool> OnPurchasedChanged;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (casinoBank == null)
            casinoBank = GetComponent<CasinoBank>();

        if (visualController == null)
            visualController = GetComponentInChildren<PurchaseVisualController>();

        _isPurchased.OnValueChanged += HandlePurchasedChanged;

        PurchaseManager.RegisterObject(this);
        ApplyPurchaseVisual();
    }

    public override void OnNetworkDespawn()
    {
        _isPurchased.OnValueChanged -= HandlePurchasedChanged;
        PurchaseManager.UnregisterObject(this);

        base.OnNetworkDespawn();
    }

    public bool CanPurchase()
    {
        if (!IsServer) return false;
        if (_isPurchased.Value) return false;
        if (casinoBank == null) return false;

        return casinoBank.CurrentBalance >= price;
    }

    [Rpc(SendTo.Server)]
    public void PurchaseServerRpc(ulong operatorId)
    {
        TryPurchase(operatorId);
    }

    public void TryPurchase(ulong operatorId)
    {
        if (!IsServer) return;
        if (_isPurchased.Value) return;
        if (!CanPurchase()) return;

        int withdrawn = casinoBank.TryWithdraw(price, operatorId, "Покупка объекта");

        if (withdrawn == price)
            _isPurchased.Value = true;
    }

    public void SetPhaseVisible(bool visible)
    {
        if (visualController == null) return;
        visualController.SetVisible(visible);
    }

    private void HandlePurchasedChanged(bool previousValue, bool newValue)
    {
        ApplyPurchaseVisual();
        OnPurchasedChanged?.Invoke(newValue);
    }

    private void ApplyPurchaseVisual()
    {
        if (visualController == null) return;
        visualController.SetPurchased(_isPurchased.Value);
    }
}