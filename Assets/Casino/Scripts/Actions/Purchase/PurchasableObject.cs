using Assets.Casino.Bank;
using Assets.Casino.PhaseDay;
using System;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Scripts.Actions.Purchase
{
    [RequireComponent(typeof(NetworkObject))]
    public class PurchasableObject : NetworkBehaviour
    {
        [Header("Item")]
        [SerializeField] private PurchaseItemDefinition itemDefinition;
        [Header("References")]
        [SerializeField] private CasinoBank casinoBank;
        [SerializeField] private GameObject unpurchasedState;
        [SerializeField] private GameObject purchasedState;
        [SerializeField] private bool isPurchasedIndividual;

        private readonly NetworkVariable<bool> _isPurchasedIndividual = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public bool IsPurchased
        {
            get
            {
                if (itemDefinition == null) return false;

                if (itemDefinition.scope == PurchaseScope.Individual)
                    return _isPurchasedIndividual.Value;

                return PurchaseManager.IsGroupPurchasedStatic(itemDefinition.groupId);
            }
        }

        public int Price => itemDefinition != null ? itemDefinition.price : 0;
        public PurchaseItemDefinition ItemDefinition => itemDefinition;

        public event Action<bool> OnPurchasedChanged;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (casinoBank == null && GameSessionManager.Instance != null)
                casinoBank = GameSessionManager.Instance.Bank;

            _isPurchasedIndividual.OnValueChanged += HandlePurchasedChanged;
            _isPurchasedIndividual.Value = isPurchasedIndividual;

            PurchaseManager.RegisterObject(this);

            // [ИЗМЕНЕНО] Подписка на глобальные покупки, если это Global-объект.
            if (itemDefinition != null && itemDefinition.scope == PurchaseScope.Global
                && PurchaseManager.Instance != null)
            {
                PurchaseManager.Instance.OnGroupPurchased += HandleGroupPurchased;
                PurchaseManager.Instance.OnGroupsReset += HandleGroupsReset;
            }

            // [ИЗМЕНЕНО] Первичный визуал. Для late-join это единственный
            // момент, когда мы подтянем уже существующие глобальные покупки —
            // NetworkList к этому моменту уже синхронизирован.
            ApplyPurchaseVisual();
        }

        public override void OnNetworkDespawn()
        {
            _isPurchasedIndividual.OnValueChanged -= HandlePurchasedChanged;
            PurchaseManager.UnregisterObject(this);

            if (itemDefinition != null && itemDefinition.scope == PurchaseScope.Global
                && PurchaseManager.Instance != null)
            {
                PurchaseManager.Instance.OnGroupPurchased -= HandleGroupPurchased;
                PurchaseManager.Instance.OnGroupsReset -= HandleGroupsReset;
            }

            base.OnNetworkDespawn();
        }

        public bool CanPurchase()
        {
            if (!IsServer) return false;
            if (IsPurchased) return false;
            if (casinoBank == null) return false;

            return casinoBank.CurrentBalance >= Price;
        }

        [Rpc(SendTo.Server)]
        public void PurchaseServerRpc(ulong operatorId)
        {
            PurchaseManager.Instance?.TryPurchase(this, operatorId);
        }

        public void SetPurchasedIndividual(bool value)
        {
            if (!IsServer) return;
            if (itemDefinition == null || itemDefinition.scope != PurchaseScope.Individual) return;
            _isPurchasedIndividual.Value = value;
        }

        // ---------------- Event handlers ----------------

        private void HandlePurchasedChanged(bool previousValue, bool newValue)
        {
            ApplyPurchaseVisual();
            OnPurchasedChanged?.Invoke(newValue);
        }

        // [НОВОЕ] Срабатывает на сервере и на всех клиентах в момент покупки группы.
        private void HandleGroupPurchased(int groupId)
        {
            if (itemDefinition == null) return;
            if (itemDefinition.scope != PurchaseScope.Global) return;
            if ((int)itemDefinition.groupId != groupId) return;

            ApplyPurchaseVisual();
            OnPurchasedChanged?.Invoke(true);
        }

        // [НОВОЕ] Сброс — пересчитываем визуал (IsPurchased вернёт false).
        private void HandleGroupsReset()
        {
            if (itemDefinition == null || itemDefinition.scope != PurchaseScope.Global) return;

            ApplyPurchaseVisual();
            OnPurchasedChanged?.Invoke(IsPurchased);
        }

        // ---------------- Visuals ----------------

        private void ApplyPurchaseVisual()
        {
            if (unpurchasedState != null) unpurchasedState.SetActive(!IsPurchased);
            if (purchasedState != null) purchasedState.SetActive(IsPurchased);
        }

        public void SetPhaseVisible(bool visibleInPhase)
        {
            if (IsPurchased)
            {
                if (purchasedState != null) purchasedState.SetActive(true);
                if (unpurchasedState != null) unpurchasedState.SetActive(false);
            }
            else
            {
                if (unpurchasedState != null) unpurchasedState.SetActive(visibleInPhase);
                if (purchasedState != null) purchasedState.SetActive(false);
            }
        }
    }
}