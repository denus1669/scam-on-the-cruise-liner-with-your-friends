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
            ApplyPurchaseVisual();
        }

        public override void OnNetworkDespawn()
        {
            _isPurchasedIndividual.OnValueChanged -= HandlePurchasedChanged;
            PurchaseManager.UnregisterObject(this);

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

        /// <summary>Только для сервера. Меняет состояние индивидуальной покупки.</summary>
        public void SetPurchased(bool value)
        {
            if (!IsServer) return;
            if (itemDefinition == null || itemDefinition.scope != PurchaseScope.Individual) return;
            _isPurchasedIndividual.Value = value;
        }

        /*
        public void TryPurchase(ulong operatorId)
        {
            if (!IsServer) return;
            if (!CanPurchase()) return;

            int withdrawn = casinoBank.TryWithdraw(Price, operatorId, "Покупка объекта");

            if (withdrawn == Price)
                _isPurchasedIndividual.Value = true;
            else Debug.LogError("withdrawn == price*?******");
        }*/

        private void HandlePurchasedChanged(bool previousValue, bool newValue)
        {
            ApplyPurchaseVisual();
            OnPurchasedChanged?.Invoke(newValue);
        }

        private void ApplyPurchaseVisual()
        {
            if (unpurchasedState != null) unpurchasedState.SetActive(!IsPurchased);
            if (purchasedState != null) purchasedState.SetActive(IsPurchased);
        }

        public void SetPhaseVisible(bool visibleInPhase)
        {
            if (IsPurchased)
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
}