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

        // Реестр купленных глобальных групп
        private readonly HashSet<string> _purchasedGroups = new();
        public event Action<string> OnGroupPurchased;

        public static bool IsGroupPurchasedStatic(string groupId)
        {
            return Instance != null && Instance.IsGroupPurchased(groupId);
        }

        // Синхронизация реестра (включая late joiners)
        private readonly NetworkVariable<string> _purchasedGroupsRaw = new(
            string.Empty, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
            if (Instance != this) return;

            base.OnNetworkSpawn();

            if (gameSessionManager == null)
                gameSessionManager = GameSessionManager.Instance;

            if (gameSessionManager != null)
            {
                if (casinoBank == null) casinoBank = gameSessionManager.Bank;
                gameSessionManager.OnStateChanged += HandleGameStateChanged;
            }
            else
                GameSessionManager.OnInstanceReady += HandleGameSessionReady;

            _purchasedGroupsRaw.OnValueChanged += HandleGroupsRawChanged;
            ApplyGroupsRaw(_purchasedGroupsRaw.Value);


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
            _purchasedGroupsRaw.OnValueChanged -= HandleGroupsRawChanged;

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

        public bool IsGroupPurchased(string groupId)
        {
            return !string.IsNullOrEmpty(groupId) && _purchasedGroups.Contains(groupId);
        }
        private void HandleGroupsRawChanged(string previous, string current) => ApplyGroupsRaw(current);

        private void ApplyGroupsRaw(string raw)
        {
            _purchasedGroups.Clear();

            if (!string.IsNullOrEmpty(raw))
            {
                foreach (var id in raw.Split(';'))
                {
                    if (!string.IsNullOrEmpty(id))
                        _purchasedGroups.Add(id);
                }
            }
        }

        private void SerializeGroups()
        {
            _purchasedGroupsRaw.Value = string.Join(";", _purchasedGroups);
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

            var def = purchasableObject.ItemDefinition;
            int price = def.price;

            if (casinoBank.CurrentBalance < price) return false;

            if (def.scope == PurchaseScope.Global)
            {
                if (string.IsNullOrEmpty(def.groupId))
                {
                    Debug.LogError($"[PurchaseManager] {def.itemId}: Global без groupId");
                    return false;
                }
                if (_purchasedGroups.Contains(def.groupId)) return false;
            }

            int withdrawn = casinoBank.TryWithdraw(price, operatorId, $"Покупка: {def.displayName}");
            if (withdrawn != price) return false;

            if (def.scope == PurchaseScope.Individual)
            {
                purchasableObject.SetPurchased(true);
            }
            else
            {
                _purchasedGroups.Add(def.groupId);
                SerializeGroups();
                OnGroupPurchased?.Invoke(def.groupId);
                NotifyGroupPurchasedClientRpc(def.groupId);
            }

            return true;
        }

        [ClientRpc]
        private void NotifyGroupPurchasedClientRpc(string groupId)
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
                if (obj != null) obj.SetPurchased(false);

            _purchasedGroups.Clear();
            SerializeGroups();
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