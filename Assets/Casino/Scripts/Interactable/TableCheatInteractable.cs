using Assets.Casino.Cheating.BlackGregCheats;
using Assets.Casino.Games;
using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Cheating
{
    /// <summary>
    /// Компонент мухлежа, висящий на СТОЛЕ (или конкретном месте за столом).
    /// Теперь он не привязан к владельцу игрока, а проверяет входящего игрока.
    /// </summary>
    /// 
    [RequireComponent(typeof(HighlighterInteractable))]
    public class TableCheatInteractable : InteractableBase
    {
        [SerializeField] GameTable thisTableComponent;

        [Header("Настройки взаимодействия")]
        [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
        [SerializeField] private int priority = 5;
        [SerializeField] private string promptText = "Мухлевать (E)";

        [SerializeField] private BlackGregCheatAction availableCheats;
        private PlayerCheatController _ownerCheatController;

        private bool _localAvailabilityCache;
        private ulong _eligibleClientId = ulong.MaxValue;

        // Ссылка на контроллер текущего владельца стола для подписки на события
        private PlayerCheatController _currentCheatController;

        public override event System.Action<bool> OnAvailabilityChanged;


        // Ссылка на контроллер мухлежа теперь берется динамически у игрока, который взаимодействует
        // Или можно иметь ссылку на общий менеджер стола, если логика общая

        public override InteractionTriggerMode TriggerMode => triggerMode;
        public override int Priority => priority;
        public override float HoldDuration => 0f;

        // Кэш для текста подсказки (опционально, можно упростить)
        private string m_CachedPrompt;
        private bool m_LastCanCheat;
        private GameObject m_LastTarget;

        public override string InteractionPromptText
        {
            get
            {
                return promptText;
            }
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (thisTableComponent != null)
            {
                thisTableComponent.OnOccupantChanged += HandleOccupantChanged;
                thisTableComponent.OnGameStateChanged += HandleGameStateChanged;

                // Инициализация для Late Joiners
                if (IsServer)
                {
                    HandleOccupantChanged(thisTableComponent.OccupiedByClientId);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (thisTableComponent != null)
            {
                thisTableComponent.OnOccupantChanged -= HandleOccupantChanged;
                thisTableComponent.OnGameStateChanged -= HandleGameStateChanged;
            }

            UnsubscribeFromPlayer();
            base.OnNetworkDespawn();
        }


        /// <summary>
        /// Проверка возможности взаимодействия.
        /// interactor — этоGameObject игрока, который нажал кнопку или навел курсор.
        /// </summary>
        public override bool CanInteract(GameObject interactor)
        {
            if (interactor == null) return false;

            // 2. Получаем компоненты игрока, который взаимодействует
            var playerManager = interactor.GetComponent<CorePlayerManager>();
            if (playerManager == null)
            {
                playerManager = interactor.GetComponentInChildren<CorePlayerManager>();
            }

            if (playerManager == null) return false;

            // 3. Проверка: сидит ли этот игрок за ЭТИМ столом
            if (GameTableManager.Instance == null) return false;

            // Получаем ID игрока, который пытается взаимодействовать
            ulong interactorId = playerManager.OwnerClientId;

            // Важно: проверяем, что игрок сидит именно за этим столом (или любым, если логика позволяет)
            // Если GetTableOccupiedByPlayer возвращает интерфейс стола, сравните его с этим объектом

            // Если у вас несколько столов, убедитесь, что игрок сидит именно за этим конкретным столом

            if (!thisTableComponent.IsGameStarted || interactorId != thisTableComponent.OccupiedByClientId) return false;

            // 4. Проверка контроллера мухлежа у этого конкретного игрока
            var cheatController = playerManager.GetComponent<PlayerCheatController>(); // Или FindObjectOfType/GetChild

            if (cheatController == null || !cheatController.CanCheat())
            {
                return false;
            }

            return true;
        }

        public override void Interact(GameObject interactor)
        {
            if (!IsSpawned) return;

            var playerManager = interactor.GetComponent<CorePlayerManager>();
            if (playerManager == null) playerManager = interactor.GetComponentInChildren<CorePlayerManager>();

            if (playerManager == null) return;

            var cheatController = playerManager.GetComponent<PlayerCheatController>();
            if (cheatController == null) return;

            Debug.Log($"[TableCheatInteractable] Игрок {playerManager.OwnerClientId} начал мухлеж! {availableCheats.CheatName} ");

            // Вызываем метод на контроллере игрока
            cheatController.RequestCheatServerRpc(availableCheats.CheatForCodeName);
        }

        private void EvaluateAndPushAvailability()
        {
            if (!IsServer || thisTableComponent == null) return;

            ulong targetClientId = ulong.MaxValue;
            bool isAvailable = false;

            // Доступно только если игра началась и стол занят
            if (thisTableComponent.IsGameStarted && thisTableComponent.IsOccupied)
            {
                targetClientId = thisTableComponent.OccupiedByClientId;

                // Проверяем, может ли этот конкретный игрок сейчас мухлевать (нет кулдауна, не мухлюет уже)
                if (_currentCheatController != null && _currentCheatController.CanCheat())
                {
                    isAvailable = true;
                }
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
        private void SubscribeToPlayer(ulong clientId)
        {
            UnsubscribeFromPlayer();

            if (clientId == ulong.MaxValue || NetworkManager.Singleton == null) return;

            var netObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
            if (netObj != null)
            {
                _currentCheatController = netObj.GetComponent<PlayerCheatController>();
                if (_currentCheatController != null)
                {
                    // ВАЖНО: Убедитесь, что событие OnCheatingStateChanged в базовом классе CheatController имеет модификатор public.
                    // Если оно protected, вам придется либо сделать его public, либо подписаться на NetworkVariable.OnValueChanged
                    _currentCheatController.OnCheatingStateChanged += HandlePlayerCheatStateChanged;
                }
            }
        }

        private void UnsubscribeFromPlayer()
        {
            if (_currentCheatController != null)
            {
                _currentCheatController.OnCheatingStateChanged -= HandlePlayerCheatStateChanged;
                _currentCheatController = null;
            }
        }

        private void HandleOccupantChanged(ulong clientId)
        {
            if (!IsServer) return;

            // При смене владельца переподписываемся на события его контроллера
            SubscribeToPlayer(clientId);
            EvaluateAndPushAvailability();
        }

        private void HandleGameStateChanged(bool isStarted)
        {
            if (!IsServer) return;
            EvaluateAndPushAvailability();
        }

        private void HandlePlayerCheatStateChanged(bool isCheating)
        {
            if (!IsServer) return;
            // Когда игрок начинает/заканчивает чит, доступность кнопки мухлежа меняется
            EvaluateAndPushAvailability();
        }
    }
}