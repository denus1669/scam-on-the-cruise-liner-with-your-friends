using Assets.Casino.Bank;
using Assets.Casino.PhaseDay;
using Assets.Casino.PhaseDay.GameStatePhase;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Scripts.Actions.Purchase
{
    [RequireComponent(typeof(NetworkObject))]
    public class PurchaseManager : NetworkBehaviour
    {
        public static PurchaseManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private GameSessionManager gameSessionManager;
        [SerializeField] private CasinoBank casinoBank;

        [Header("Phase visibility")]
        [SerializeField] private GameState visibleState = GameState.Preparing;
        private readonly HashSet<PurchaseGroupId> _purchasedGroups = new();

        // NetworkList<int> для синхронизации с клиентами (включая Late Joiners)
        private readonly NetworkList<int> _purchasedGroupsNet = new(
            null,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        // Реестр купленных глобальных групп
        public event Action<int> OnGroupPurchased;

        public static bool IsGroupPurchasedStatic(PurchaseGroupId groupId)
        {
            return Instance != null && Instance.IsGroupPurchased(groupId);
        }

        private readonly HashSet<PurchasableObject> _purchasedObjects = new();

        public static void RegisterObject(PurchasableObject purchasableObject)
        {
            if (Instance == null || purchasableObject == null) return;
            Instance.Register(purchasableObject);
        }

        public static void UnregisterObject(PurchasableObject purchasableObject)
        {
            if (Instance == null || purchasableObject == null) return;
            Instance.Unregister(purchasableObject);
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
            base.OnNetworkSpawn();

            if (Instance != this) return;


            if (gameSessionManager == null)
                gameSessionManager = GameSessionManager.Instance;

            if (gameSessionManager != null)
            {
                if (casinoBank == null) casinoBank = gameSessionManager.Bank;
                gameSessionManager.OnStateChanged += HandleGameStateChanged;
            }
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

            if (casinoBank == null) casinoBank = gameSessionManager.Bank;
            gameSessionManager.OnStateChanged += HandleGameStateChanged;
            ApplyCurrentPhase();
        }

        // ---------------- Реестр групп ----------------

        public bool IsGroupPurchased(PurchaseGroupId groupId)
        {
            return !(groupId == 0) && _purchasedGroups.Contains(groupId);
        }

        // ---------------- Покупка ----------------

        /// <summary>
        /// Единая точка покупки. Вызывается только на сервере.
        /// </summary>
        public bool TryPurchase(PurchasableObject purchasableObject, ulong operatorId)
        {
            if (!IsServer || purchasableObject == null || purchasableObject.ItemDefinition == null) return false;
            if (purchasableObject.IsPurchased) return false;
            if (casinoBank == null) return false;

            var itemDef = purchasableObject.ItemDefinition;
            int price = itemDef.price;

            if (casinoBank.CurrentBalance < price) return false;

            if (itemDef.scope == PurchaseScope.Global)
            {
                if ((itemDef.groupId) == 0)
                {
                    Debug.LogError($"[PurchaseManager] {itemDef.itemId}: Global без groupId");
                    return false;
                }
                if (_purchasedGroups.Contains(itemDef.groupId)) return false;
            }

            int withdrawn = casinoBank.TryWithdraw(price, operatorId, $"Покупка: {itemDef.displayName}");
            if (withdrawn != price) return false;

            if (itemDef.scope == PurchaseScope.Individual)
            {
                purchasableObject.SetPurchasedIndividual(true);
            }
            else
            {
                _purchasedGroups.Add(itemDef.groupId);
                OnGroupPurchased?.Invoke((int)itemDef.groupId);
                NotifyGroupPurchasedClientRpc((int)itemDef.groupId);
            }

            return true;
        }

        [ClientRpc]
        private void NotifyGroupPurchasedClientRpc(int groupId)
        {
            if (IsServer) return; // сервер уже вызвал локально
            OnGroupPurchased?.Invoke(groupId);
        }

        /// <summary>
        /// Полный сброс покупок (новый день/сессия). Вызывать на сервере.
        /// </summary>
        public void ResetAllPurchases()
        {
            if (!IsServer) return;

            foreach (var obj in _purchasedObjects)
                if (obj != null) obj.SetPurchasedIndividual(false);

            _purchasedGroups.Clear();
        }

        // ---------------- Фаза / видимость ----------------

        private void CollectSceneObjects()
        {
            var objects = FindObjectsByType<PurchasableObject>(FindObjectsSortMode.None);

            foreach (var obj in objects)
                Register(obj);
        }

        private void Register(PurchasableObject purchasableObject)
        {
            if (purchasableObject == null) return;

            _purchasedObjects.Add(purchasableObject);
            ApplyPhaseToObject(purchasableObject);
        }

        private void Unregister(PurchasableObject purchasableObject)
        {
            if (purchasableObject == null) return;
            _purchasedObjects.Remove(purchasableObject);
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

            _purchasedObjects.RemoveWhere(x => x == null);

            foreach (var purchasableObject in _purchasedObjects)
                purchasableObject.SetPhaseVisible(visible);
        }

        private void ApplyPhaseToObject(PurchasableObject purchasableObject)
        {
            if (purchasableObject == null) return;
            if (gameSessionManager == null) return;

            purchasableObject.SetPhaseVisible(gameSessionManager.CurrentState == visibleState);
        }
    }
}