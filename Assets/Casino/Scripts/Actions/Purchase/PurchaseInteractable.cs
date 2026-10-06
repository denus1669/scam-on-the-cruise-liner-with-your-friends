using Assets.Casino.Bank;
using Assets.Casino.Games.BlackGreg;
using Assets.Casino.PhaseDay;
using Assets.Casino.PhaseDay.GameStatePhase;
using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Scripts.Actions.Purchase
{
    public class PurchaseInteractable : InteractableBase
    {
        [Header("Ссылки")]
        [SerializeField] private PurchasableObject purchasableObject;

        [Header("Настройки")]
        [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
        [SerializeField] private int priority = 5;
        [SerializeField] private float _timeToHold = 1f;
        [SerializeField] private string promptText = "Купить (E)";
        [SerializeField] private string promptTextNoMoney = "Недостаточно средств";
        [SerializeField] private string promptTextWrongPhase = "Покупка недоступна";
        [SerializeField] private string promptTextAlreadyPurchased = "Уже куплено";

        public override event System.Action<bool> OnAvailabilityChanged;

        private GameSessionManager _sessionManager;
        private CasinoBank _bank;
        private bool _isAvailable; // можно купить (для подсветки)
        private bool _isInteractable;   // можно взаимодействовать (показывать промпт)

        public override InteractionTriggerMode TriggerMode => triggerMode;
        public override int Priority => priority;
        public override float HoldDuration => _timeToHold;

        public override string InteractionPromptText
        {
            get
            {
                if (purchasableObject == null) return "Объект недоступен";
                if (purchasableObject.IsPurchased) return promptTextAlreadyPurchased;
                if (_sessionManager != null && _sessionManager.CurrentState != GameState.Preparing) return promptTextWrongPhase;
                if (_bank != null && _bank.CurrentBalance < purchasableObject.Price) return $"{purchasableObject.Price}$ ({promptTextNoMoney})";

                return $"{promptText} {purchasableObject.ItemDefinition.displayName} ({purchasableObject.Price}$)";
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (purchasableObject == null)
                purchasableObject = GetComponent<PurchasableObject>();

            if (GameSessionManager.Instance != null)
                Initialize(GameSessionManager.Instance);
            else
                GameSessionManager.OnInstanceReady += HandleInstanceReady;
        }

        private void HandleInstanceReady()
        {
            if (GameSessionManager.Instance != null)
                Initialize(GameSessionManager.Instance);
        }

        private void Initialize(GameSessionManager sessionManager)
        {
            _sessionManager = sessionManager;
            _bank = sessionManager.Bank;

            // Подписываемся на изменения, чтобы мгновенно обновлять доступность на клиенте
            _sessionManager.OnStateChanged += HandleStateChanged;

            if (_bank != null)
                _bank.OnBalanceChanged += HandleBalanceChanged;

            if (purchasableObject != null)
                purchasableObject.OnPurchasedChanged += HandlePurchasedChanged;

            EvaluateAvailability();
        }

        public override void OnNetworkDespawn()
        {
            if (_sessionManager != null) _sessionManager.OnStateChanged -= HandleStateChanged;
            if (_bank != null) _bank.OnBalanceChanged -= HandleBalanceChanged;
            if (purchasableObject != null) purchasableObject.OnPurchasedChanged -= HandlePurchasedChanged;

            GameSessionManager.OnInstanceReady -= HandleInstanceReady;
            base.OnNetworkDespawn();
        }

        private void HandleStateChanged(GameState state) => EvaluateAvailability();
        private void HandleBalanceChanged(int oldBalance, int newBalance) => EvaluateAvailability();
        private void HandlePurchasedChanged(bool isPurchased) => EvaluateAvailability();

        private void EvaluateAvailability()
        {
            if (purchasableObject == null || purchasableObject.ItemDefinition == null)
            {
                SetInteractable(false);
                SetAvailable(false);
                return;
            }

            bool inRightPhase = _sessionManager != null
                                && _sessionManager.CurrentState == GameState.Preparing;
            bool notPurchased = !purchasableObject.IsPurchased;
            bool hasMoney = _bank != null
                                && _bank.CurrentBalance >= purchasableObject.Price;

            // Показывать промпт: фаза правильная и объект ещё не куплен
            // (деньги тут не учитываем — иначе не увидим «Недостаточно средств»)
            SetInteractable(inRightPhase && notPurchased);

            // Подсветка: то же самое + хватает денег
            SetAvailable(inRightPhase && notPurchased && hasMoney);
        }

        private void SetAvailable(bool value)
        {
            if (_isAvailable == value) return;
            _isAvailable = value;
            OnAvailabilityChanged?.Invoke(_isAvailable); // это подсветка
        }

        private void SetInteractable(bool value)
        {
            if (_isInteractable == value) return;
            _isInteractable = value;
            // если у базового класса есть отдельное событие для промпта — дерните его здесь
        }

        public override bool CanInteract(GameObject interactor) => _isInteractable;
        public override bool HasAnyAvailableInteractor() => _isInteractable;

        public override void Interact(GameObject interactor)
        {
            if (!IsSpawned || purchasableObject == null || !_isAvailable) return;

            var playerManager = interactor.GetComponent<CorePlayerManager>();
            if (playerManager == null) playerManager = interactor.GetComponentInChildren<CorePlayerManager>();

            if (playerManager != null)
            {
                purchasableObject.PurchaseServerRpc(playerManager.OwnerClientId);
            }
        }
    }
}