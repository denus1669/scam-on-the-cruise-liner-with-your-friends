using Assets.Casino.Bank;
using Assets.Casino.PhaseDay;
using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PurchasableObject : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CasinoBank casinoBank;
    [SerializeField] private GameObject unpurchasedState;
    [SerializeField] private GameObject purchasedState;

    [Header("Settings")]
    [SerializeField] private int price = 100;

    private readonly NetworkVariable<bool> _isPurchased = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private bool _visible = true;

    public bool IsPurchased => _isPurchased.Value;
    public int Price => price;

    public event Action<bool> OnPurchasedChanged;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (casinoBank == null && GameSessionManager.Instance != null)
            casinoBank = GameSessionManager.Instance.Bank;

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

    private void HandlePurchasedChanged(bool previousValue, bool newValue)
    {
        ApplyPurchaseVisual();
        OnPurchasedChanged?.Invoke(newValue);
    }

    private void ApplyPurchaseVisual()
    {
        if (unpurchasedState != null) unpurchasedState.SetActive(!_isPurchased.Value);
        if (purchasedState != null) purchasedState.SetActive(_isPurchased.Value);
    }

    public void SetPhaseVisible(bool visibleInPhase)
    {
        if (_isPurchased.Value)
        {
            // Купленные объекты видны всегда
            if (purchasedState != null) purchasedState.SetActive(true);
            if (unpurchasedState != null) unpurchasedState.SetActive(false);
        }
        else
        {
            // Не купленные подчиняются фазе
            if (unpurchasedState != null) unpurchasedState.SetActive(visibleInPhase);
            if (purchasedState != null) purchasedState.SetActive(false);
        }
    }
}