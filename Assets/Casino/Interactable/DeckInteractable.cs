using Assets.Casino.Games.BlackGreg;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Interactable
{
    [RequireComponent(typeof(HighlighterInteractable))]
    public class DeckInteractable : InteractableBase
    {
        [Header("Стол")]
        [SerializeField] private BlackGregTable blackGregTable;

        [Header("Настройки взаимодействия")]
        [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
        [SerializeField] private int priority = 0;
        [SerializeField] private string promptText = "Взять карту (E)";

        private bool _localAvailabilityCache;
        private readonly HashSet<ulong> _highlightedClients = new();


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

            // 1. Считаем новое множество тех, кому должно быть доступно
            var newTargets = new HashSet<ulong>();

            if (blackGregTable.IsBotReachedTable)
            {
                if (blackGregTable.IsOccupied)
                {
                    // Доступно только тому, кто занял, и только если он ещё в зоне
                    if (blackGregTable.playersInGameArea.Contains(blackGregTable.OccupiedByClientId))
                        newTargets.Add(blackGregTable.OccupiedByClientId);
                }
                else
                {
                    // Свободен — доступно всем в зоне
                    foreach (var id in blackGregTable.playersInGameArea)
                        newTargets.Add(id);
                }
            }

            // 2. Гасим тех, кто был подсвечен, но больше не должен
            foreach (var id in _highlightedClients)
            {
                if (!newTargets.Contains(id))
                    UpdateAvailabilityClientRpc(false, RpcTarget.Single(id, RpcTargetUse.Temp));
            }

            // 3. Включаем/обновляем тех, кто должен быть подсвечен
            foreach (var id in newTargets)
            {
                // Если нужно слать только при изменении — оборачиваем в проверку
                if (!_highlightedClients.Contains(id))
                    UpdateAvailabilityClientRpc(true, RpcTarget.Single(id, RpcTargetUse.Temp));
            }

            // 4. Синхронизируем состояние
            _highlightedClients.Clear();
            foreach (var id in newTargets)
                _highlightedClients.Add(id);
        }

        /// <summary>
        /// Отправляет статус доступности конкретному клиенту.
        /// </summary>
        [Rpc(SendTo.SpecifiedInParams)]
        private void UpdateAvailabilityClientRpc(bool isAvailable, RpcParams rpcParams = default)
        {
            if (_localAvailabilityCache != isAvailable)
            {
                _localAvailabilityCache = isAvailable;
                OnAvailabilityChanged?.Invoke(_localAvailabilityCache); // Пуш в DeckHighlighter
            }
        }

        public override bool HasAnyAvailableInteractor() => _localAvailabilityCache;

        public override bool CanInteract(GameObject interactor)
        {
            if (blackGregTable == null || !blackGregTable.IsBotReachedTable)
            {
                Debug.Log($"(blackGregTable == {blackGregTable == null} || blackGregTable.IsOccupied == {blackGregTable.IsOccupied} || !blackGregTable.IsBotReachedTable == {!blackGregTable.IsBotReachedTable}");
                return false;
            }

            ulong clientId = interactor.GetComponent<NetworkObject>().OwnerClientId;

            if (blackGregTable.IsOccupied && blackGregTable.OccupiedByClientId != clientId)
            {
                Debug.Log($"blackGregTable.IsOccupied == {blackGregTable.IsOccupied} && blackGregTable.OccupiedByClientId != clientId == {blackGregTable.OccupiedByClientId != clientId}");
                return false;
            }
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