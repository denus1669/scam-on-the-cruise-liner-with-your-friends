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

        // [ИЗМЕНЕНО] Единственный источник правды для глобальных покупок.
        // Синхронизируется автоматически, включая late-join.
        private readonly NetworkList<int> _purchasedGroupsNet = new(
            null,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly HashSet<PurchasableObject> _purchasedObjects = new();

        /// <summary>Вызывается при добавлении группы (на сервере и на всех клиентах).</summary>
        public event Action<int> OnGroupPurchased;

        /// <summary>Вызывается при полном сбросе покупок (на сервере и на всех клиентах).</summary>
        public event Action OnGroupsReset;

        // ---------------- Public API ----------------

        public static bool IsGroupPurchasedStatic(PurchaseGroupId groupId)
            => Instance != null && Instance.IsGroupPurchased(groupId);

        public bool IsGroupPurchased(PurchaseGroupId groupId)
        {
            if (groupId == 0) return false;
            // [ИЗМЕНЕНО] Читаем из NetworkList — работает и на сервере, и на клиентах.
            return _purchasedGroupsNet.Contains((int)groupId);
        }

        public static void RegisterObject(PurchasableObject obj)
        {
            if (Instance != null && obj != null) Instance.Register(obj);
        }

        public static void UnregisterObject(PurchasableObject obj)
        {
            if (Instance != null && obj != null) Instance.Unregister(obj);
        }

        // ---------------- Lifecycle ----------------

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (Instance != this) return;

            // [ИЗМЕНЕНО] Подписываемся на изменения NetworkList.
            // Событие придёт и на сервере (после Add), и на всех клиентах.
            _purchasedGroupsNet.OnListChanged += HandlePurchasedGroupsChanged;

            if (gameSessionManager == null)
                gameSessionManager = GameSessionManager.Instance;

            if (gameSessionManager != null)
            {
                if (casinoBank == null) casinoBank = gameSessionManager.Bank;
                gameSessionManager.OnStateChanged += HandleGameStateChanged;
            }
            else
            {
                GameSessionManager.OnInstanceReady += HandleGameSessionReady;
            }

            CollectSceneObjects();
            ApplyCurrentPhase();
        }

        public override void OnNetworkDespawn()
        {
            if (Instance != this) { base.OnNetworkDespawn(); return; }

            _purchasedGroupsNet.OnListChanged -= HandlePurchasedGroupsChanged;

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

        // ---------------- NetworkList change handling ----------------

        private void HandlePurchasedGroupsChanged(NetworkListEvent<int> changeEvent)
        {
            switch (changeEvent.Type)
            {
                case NetworkListEvent<int>.EventType.Add:
                case NetworkListEvent<int>.EventType.Insert:
                    OnGroupPurchased?.Invoke(changeEvent.Value);
                    break;

                case NetworkListEvent<int>.EventType.Clear:
                    OnGroupsReset?.Invoke();
                    break;
            }
        }

        // ---------------- Purchase ----------------

        /// <summary>Единая точка покупки. Вызывается только на сервере.</summary>
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
                if ((int)itemDef.groupId == 0)
                {
                    Debug.LogError($"[PurchaseManager] {itemDef.itemId}: Global без groupId");
                    return false;
                }
                if (_purchasedGroupsNet.Contains((int)itemDef.groupId)) return false;
            }

            int withdrawn = casinoBank.TryWithdraw(price, operatorId, $"Покупка: {itemDef.displayName}");
            if (withdrawn != price) return false;

            if (itemDef.scope == PurchaseScope.Individual)
            {
                purchasableObject.SetPurchasedIndividual(true);
            }
            else
            {
                // [ИЗМЕНЕНО] NetworkList сам разошлёт всем.
                // OnGroupPurchased сработает через OnListChanged и на сервере, и на клиентах.
                _purchasedGroupsNet.Add((int)itemDef.groupId);
            }

            return true;
        }

        /// <summary>Полный сброс покупок (новый день/сессия). Только сервер.</summary>
        public void ResetAllPurchases()
        {
            if (!IsServer) return;

            // Индивидуальные — точечно.
            foreach (var obj in _purchasedObjects)
                if (obj != null) obj.SetPurchasedIndividual(false);

            // [ИЗМЕНЕНО] Clear вызовет OnListChanged(Clear) на всех клиентах.
            // PurchasableObject рефрешнёт визуал через OnGroupsReset.
            _purchasedGroupsNet.Clear();
        }

        // ---------------- Phase / visibility ----------------

        private void CollectSceneObjects()
        {
            var objects = FindObjectsByType<PurchasableObject>(FindObjectsSortMode.None);
            foreach (var obj in objects) Register(obj);
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

        private void HandleGameStateChanged(GameState newState) => ApplyPhase(newState);

        private void ApplyCurrentPhase()
        {
            if (gameSessionManager == null) return;
            ApplyPhase(gameSessionManager.CurrentState);
        }

        private void ApplyPhase(GameState state)
        {
            bool visible = state == visibleState;
            _purchasedObjects.RemoveWhere(x => x == null);
            foreach (var obj in _purchasedObjects)
                obj.SetPhaseVisible(visible);
        }

        private void ApplyPhaseToObject(PurchasableObject obj)
        {
            if (obj == null || gameSessionManager == null) return;
            obj.SetPhaseVisible(gameSessionManager.CurrentState == visibleState);
        }
    }
}