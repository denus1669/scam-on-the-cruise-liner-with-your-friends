using Assets.Casino.PhaseDay;
using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;
using Assets.Casino.PhaseDay.GameStatePhase;

namespace Assets.Casino.PhaseDay.GameStateActivate
{
    [RequireComponent(typeof(HighlighterInteractable))]
    public class DayActiveInteractable : InteractableBase
    {
        [Header("Настройки взаимодействия")]
        [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
        [SerializeField] private int priority = 10;
        [SerializeField] private string promptText = "Начать день (E)";

        [SerializeField] private GameObject _firstDoorPart;
        [SerializeField] private GameObject _doorOpenPart;
        [SerializeField] private float _pivotPoint;

        // Локальный кэш доступности
        private bool _localAvailabilityCache;

        // Событие для Highlighter
        public override event System.Action<bool> OnAvailabilityChanged;
        private string m_CachedPrompt;
        private GameState m_LastState;

        public override InteractionTriggerMode TriggerMode => triggerMode;
        public override int Priority => priority;
        public override float HoldDuration => 0f;

        public override string InteractionPromptText
        {
            get
            {
                var manager = GameSessionManager.Instance;
                var currentPhase = manager != null ? manager.CurrentState : GameState.Preparing;

                if (currentPhase != m_LastState || m_CachedPrompt == null)
                {
                    m_LastState = currentPhase;
                    m_CachedPrompt = currentPhase switch
                    {
                        GameState.Preparing => promptText,
                        GameState.DayActive => "Идёт игровой день...",
                        _ => promptText
                    };
                }

                return m_CachedPrompt;
            }
        }

        public override bool CanInteract(GameObject interactor)
        {
            var manager = GameSessionManager.Instance;
            if (manager == null) return false;

            if (!interactor.TryGetComponent<NetworkObject>(out var netObj)) return false;
            if (!netObj.IsPlayerObject) return false;

            return manager.CurrentState == GameState.Preparing;
        }

        public override void Interact(GameObject interactor)
        {
            if (!IsSpawned) return;

            var manager = GameSessionManager.Instance;
            if (manager == null) return;

            if (manager.CurrentState != GameState.Preparing)
            {
                Debug.Log("[DayActiveInteractable] Попытка взаимодействия вне фазы Preparation");
                return;
            }

            Debug.Log("[DayActiveInteractable] Игрок запрашивает начало дня!");
            manager.StartDayServerRpc();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Подписываемся на событие смены фазы в менеджере
            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.OnStateChanged += HandleGameStateChanged;
                // Синхронизируем начальное состояние для поздно подключившихся
                HandleGameStateChanged(GameSessionManager.Instance.CurrentState);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.OnStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            // 1. Обновляем локальный кэш доступности
            UpdateAvailabilityCache(newState);

            // 2. Обновляем визуал двери
            if (newState == GameState.DayActive)
            {
                OpenDoorVisuals();
            }
            else if (newState == GameState.Preparing)
            {
                CloseDoorVisuals();
            }
        }
        private void OpenDoorVisuals()
        {
            if (_firstDoorPart != null) _firstDoorPart.SetActive(false);
            if (_doorOpenPart != null) _doorOpenPart.transform.localEulerAngles = new Vector3(0, _pivotPoint, 0);
        }

        private void CloseDoorVisuals()
        {
            if (_firstDoorPart != null) _firstDoorPart.SetActive(true);
            if (_doorOpenPart != null) _doorOpenPart.transform.localEulerAngles = Vector3.zero;
        }

        /// <summary>
        /// Вычисляет доступность локально на клиенте.
        /// Так как GameState синхронизируется через NetworkVariable, 
        /// это значение всегда совпадает с серверным без каких-либо RPC.
        /// </summary>
        private void UpdateAvailabilityCache(GameState currentState)
        {
            bool isAvailable = currentState == GameState.Preparing;

            if (_localAvailabilityCache != isAvailable)
            {
                _localAvailabilityCache = isAvailable;
                OnAvailabilityChanged?.Invoke(_localAvailabilityCache); // Пуш в Highlighter
            }
        }

        /// <summary>
        /// Возвращает true, если сейчас фаза подготовки и любой игрок может начать день.
        /// Используется подсветкой доступности на сервере.
        /// </summary>
        public override bool HasAnyAvailableInteractor() => _localAvailabilityCache;

    }
}