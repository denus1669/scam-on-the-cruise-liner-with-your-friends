using Assets.Casino.Games.BlackGreg;
using System;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Interactable
{
    [RequireComponent(typeof(DeckHighlighter))]
    public class DeckInteractable : InteractableBase
    {
        [Header("Стол")]
        [SerializeField] private BlackGregTable blackGregTable;

        [Header("Настройки взаимодействия")]
        [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
        [SerializeField] private int priority = 0;
        [SerializeField] private string promptText = "Взять карту (E)";

        private bool _localAvailabilityCache;
        private ulong _eligibleClientId = ulong.MaxValue;

        public override event Action<bool> OnAvailabilityChanged;

        public override InteractionTriggerMode TriggerMode => triggerMode;
        public override int Priority => priority;
        public override string InteractionPromptText => promptText;
        public override float HoldDuration => 0f;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (blackGregTable != null)
            {
                // Подписываемся на встроенные события GameTable
                blackGregTable.OnOccupantChanged += HandleOccupantChanged;
                blackGregTable.OnBotReachedTableStateChanged += HandleBotReachedTableChanged;
                blackGregTable.playersInGameArea.OnListChanged += HandlePlayersInAreaChanged;
            }

            // Первичный расчет на сервере при спавне
            if (IsServer) EvaluateAndPushAvailability();
        }

        public override void OnNetworkDespawn()
        {
            if (blackGregTable != null)
            {
                blackGregTable.OnOccupantChanged -= HandleOccupantChanged;
                blackGregTable.OnBotReachedTableStateChanged -= HandleBotReachedTableChanged;
                blackGregTable.playersInGameArea.OnListChanged -= HandlePlayersInAreaChanged;
            }
            base.OnNetworkDespawn();
        }

        private void HandleOccupantChanged(ulong clientId) => EvaluateAndPushAvailability();
        private void HandleBotReachedTableChanged(bool reached) => EvaluateAndPushAvailability();
        private void HandlePlayersInAreaChanged(NetworkListEvent<ulong> change) => EvaluateAndPushAvailability();

        /// <summary>
        /// Серверная логика: вычисляет, кому доступна колода, и пушит состояние конкретному клиенту.
        /// </summary>
        private void EvaluateAndPushAvailability()
        {
            if (!IsServer || blackGregTable == null) return;

            ulong targetClientId = ulong.MaxValue;
            bool isAvailable = false;

            if (blackGregTable.IsBotReachedTable)
            {
                if (blackGregTable.IsOccupied)
                {
                    if (blackGregTable.playersInGameArea.Contains(blackGregTable.OccupiedByClientId))
                    {
                        targetClientId = blackGregTable.OccupiedByClientId;
                        isAvailable = true;
                    }
                }
                else if (blackGregTable.playersInGameArea.Count > 0)
                {
                    targetClientId = blackGregTable.playersInGameArea[0];
                    isAvailable = true;
                }
            }

            // Если целевой игрок сменился, гасим подсветку у старого
            if (_eligibleClientId != targetClientId && _eligibleClientId != ulong.MaxValue)
            {
                UpdateAvailabilityClientRpc(false, RpcTarget.Single(_eligibleClientId, RpcTargetUse.Temp));
            }

            _eligibleClientId = targetClientId;

            // Отправляем актуальный статус новому (или текущему) целевому игроку
            if (targetClientId != ulong.MaxValue)
            {
                UpdateAvailabilityClientRpc(isAvailable, RpcTarget.Single(targetClientId, RpcTargetUse.Temp));
            }
        }

        /// <summary>
        /// Отправляет статус доступности конкретному клиенту.
        /// </summary>
        [Rpc(SendTo.SpecifiedInParams)]
        private void UpdateAvailabilityClientRpc(bool isAvailable, RpcParams rpcParams = default)
        {
            // Так как RPC пришел только целевому клиенту, строгая проверка LocalClientId больше не нужна,
            // но можно оставить для paranoia-безопасности.
            if (NetworkManager.Singleton.LocalClientId != rpcParams.Receive.SenderClientId)
            {
                // В редких случаях (например, смена владельца объекта) может сработать.
            }

            if (_localAvailabilityCache != isAvailable)
            {
                _localAvailabilityCache = isAvailable;
                OnAvailabilityChanged?.Invoke(_localAvailabilityCache); // Пуш в DeckHighlighter
            }
        }

        public override bool HasAnyAvailableInteractor() => _localAvailabilityCache;

        public override bool CanInteract(GameObject interactor)
        {
            if (blackGregTable == null || !blackGregTable.IsBotReachedTable) return false;
            if (!interactor.TryGetComponent<NetworkObject>(out var netObj)) return false;

            ulong clientId = netObj.OwnerClientId;

            // Жесткая серверная проверка: игрок должен быть тем самым "eligible"
            if (clientId != _eligibleClientId) return false;

            if (blackGregTable.IsOccupied && blackGregTable.OccupiedByClientId != clientId) return false;

            return blackGregTable.playersInGameArea.Contains(clientId);
        }

        public override void Interact(GameObject interactor)
        {
            if (!IsSpawned || blackGregTable == null) return;
            if (!interactor.TryGetComponent<NetworkObject>(out var netObj)) return;

            blackGregTable.RequestDrawCardServerRpc(netObj.OwnerClientId);
        }
    }
}