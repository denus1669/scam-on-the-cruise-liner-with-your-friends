using Assets.Casino.Bank;
using Assets.Casino.PhaseDay;
using Assets.Casino.PhaseDay.GameStatePhase;
using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PurchasableObject))]
public class PurchaseInteractable : InteractableBase
{
    [Header("Ссылки")]
    [SerializeField] private PurchasableObject purchasableObject;

    [Header("Настройки взаимодействия")]
    [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
    [SerializeField] private int priority = 5;
    [SerializeField] private string promptText = "Купить (E)";
    [SerializeField] private string promptTextNoMoney = "Недостаточно средств";
    [SerializeField] private string promptTextWrongPhase = "Покупка недоступна";
    [SerializeField] private string promptTextAlreadyPurchased = "Уже куплено";

    public override event System.Action<bool> OnAvailabilityChanged;

    private bool _localAvailabilityCache;
    private ulong _eligibleClientId = ulong.MaxValue;

    // Кэш для оптимизации обновления текста подсказки
    private string m_CachedPrompt;
    private bool m_LastIsPurchased;
    private bool m_LastHasMoney;
    private bool m_LastCorrectPhase;

    private GameSessionManager _sessionManager;
    private CasinoBank _bank;

    public override InteractionTriggerMode TriggerMode => triggerMode;
    public override int Priority => priority;
    public override float HoldDuration => 0f;

    /// <summary>
    /// Динамический текст подсказки.
    /// </summary>
    public override string InteractionPromptText
    {
        get
        {
            if (purchasableObject == null) return "Объект недоступен";

            bool isPurchased = purchasableObject.IsPurchased;
            bool hasMoney = _bank != null && _bank.CurrentBalance >= purchasableObject.Price;
            bool correctPhase = _sessionManager != null && _sessionManager.CurrentState == GameState.Preparing;

            if (isPurchased != m_LastIsPurchased || hasMoney != m_LastHasMoney ||
                correctPhase != m_LastCorrectPhase || m_CachedPrompt == null)
            {
                m_LastIsPurchased = isPurchased;
                m_LastHasMoney = hasMoney;
                m_LastCorrectPhase = correctPhase;

                m_CachedPrompt = isPurchased ? promptTextAlreadyPurchased :
                    !correctPhase ? promptTextWrongPhase :
                    !hasMoney ? promptTextNoMoney :
                    $"{promptText} ({purchasableObject.Price}$)";
            }

            return m_CachedPrompt;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (purchasableObject == null)
            purchasableObject = GetComponent<PurchasableObject>();

        if (GameSessionManager.Instance != null)
        {
            Initialize(GameSessionManager.Instance);
        }
        else
        {
            GameSessionManager.OnInstanceReady += HandleInstanceReady;
        }
    }

    private void HandleInstanceReady()
    {
        if (GameSessionManager.Instance != null)
        {
            Initialize(GameSessionManager.Instance);
        }
    }

    private void Initialize(GameSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
        _bank = sessionManager.Bank;

        _sessionManager.OnStateChanged += HandleStateChanged;

        if (_bank != null)
        {
            _bank.OnBalanceChanged += HandleBalanceChanged;
        }

        if (purchasableObject != null)
        {
            purchasableObject.OnPurchasedChanged += HandlePurchasedChanged;
        }

        if (IsServer) EvaluateAndPushAvailability();
    }

    public override void OnNetworkDespawn()
    {
        if (_sessionManager != null)
        {
            _sessionManager.OnStateChanged -= HandleStateChanged;
        }

        if (_bank != null)
        {
            _bank.OnBalanceChanged -= HandleBalanceChanged;
        }

        if (purchasableObject != null)
        {
            purchasableObject.OnPurchasedChanged -= HandlePurchasedChanged;
        }

        GameSessionManager.OnInstanceReady -= HandleInstanceReady;

        base.OnNetworkDespawn();
    }

    private void HandleStateChanged(GameState state)
    {
        if (IsServer) EvaluateAndPushAvailability();
    }

    private void HandleBalanceChanged(int oldBalance, int newBalance)
    {
        if (IsServer) EvaluateAndPushAvailability();
    }

    private void HandlePurchasedChanged(bool isPurchased)
    {
        if (IsServer) EvaluateAndPushAvailability();
    }

    private void EvaluateAndPushAvailability()
    {
        if (!IsServer || purchasableObject == null) return;

        ulong targetClientId = ulong.MaxValue;
        bool isAvailable = false;

        // Проверяем условия доступности
        if (!purchasableObject.IsPurchased &&
            _sessionManager != null &&
            _sessionManager.CurrentState == GameState.Preparing &&
            _bank != null &&
            _bank.CurrentBalance >= purchasableObject.Price)
        {
            // Покупка доступна всем игрокам (или можно ограничить определенными условиями)
            // Здесь можно добавить проверку на конкретного игрока, если нужно
            targetClientId = NetworkManager.Singleton.LocalClientId; // Временно для тестирования
            isAvailable = true;
        }

        // Гасим подсветку у старого eligible игрока
        if (_eligibleClientId != targetClientId && _eligibleClientId != ulong.MaxValue)
        {
            UpdateAvailabilityClientRpc(false, RpcTarget.Single(_eligibleClientId, RpcTargetUse.Temp));
        }

        _eligibleClientId = targetClientId;

        // Включаем подсветку у нового eligible игрока
        if (targetClientId != ulong.MaxValue)
        {
            UpdateAvailabilityClientRpc(isAvailable, RpcTarget.Single(targetClientId, RpcTargetUse.Temp));
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void UpdateAvailabilityClientRpc(bool isAvailable, RpcParams rpcParams = default)
    {
        if (_localAvailabilityCache != isAvailable)
        {
            _localAvailabilityCache = isAvailable;
            OnAvailabilityChanged?.Invoke(_localAvailabilityCache);
        }
    }

    public override bool HasAnyAvailableInteractor() => _localAvailabilityCache;

    /// <summary>
    /// Определяет, может ли объект быть в фокусе.
    /// </summary>
    public override bool CanInteract(GameObject interactor)
    {
        return _localAvailabilityCache;
    }

    /// <summary>
    /// Выполняется после успешного нажатия кнопки.
    /// </summary>
    public override void Interact(GameObject interactor)
    {
        if (!IsSpawned || purchasableObject == null)
            return;

        if (!_localAvailabilityCache)
            return;

        var playerManager = interactor.GetComponent<CorePlayerManager>();
        if (playerManager == null) playerManager = interactor.GetComponentInChildren<CorePlayerManager>();

        if (playerManager != null)
        {
            purchasableObject.PurchaseServerRpc(playerManager.OwnerClientId);
        }
    }
}