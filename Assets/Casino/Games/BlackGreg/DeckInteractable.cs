using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Games.BlackGreg
{

    /// <summary>
    /// Интерактивный объект "Колода карт".
    /// При взаимодействии даёт команду столу выдать карту игроку.
    /// </summary>
    public class DeckInteractable : NetworkBehaviour, IInteractable
    {
        [Header("Стол")]
        [SerializeField] private BlackGregTable blackGregTable;

        [Header("Настройки взаимодействия")]
        [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
        [SerializeField] private int priority = 0;
        [SerializeField] private string promptText = "Взять карту (E)";

        public InteractionTriggerMode TriggerMode => triggerMode;
        public int Priority => priority;
        public string InteractionPromptText => promptText;

        public float HoldDuration => 0f;

        public bool CanInteract(GameObject interactor)
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

            Debug.Log($"blackGregTable.playersInGameArea.Contains(clientId) == {blackGregTable.playersInGameArea.Contains(clientId)}");
            return blackGregTable.playersInGameArea.Contains(clientId);
        }

        public void Interact(GameObject interactor)
        {

            if (!IsSpawned || blackGregTable == null)
            {
                return;
            }

            ulong clientId = interactor.GetComponent<NetworkObject>().OwnerClientId;
            blackGregTable.RequestDrawCardServerRpc(clientId);
            Debug.Log($"{clientId}");
        }

        /// <summary>
        /// Возвращает true, если хотя бы один игрок сейчас может взаимодействовать с колодой.
        ///
        /// Этот метод используется подсветкой доступности.
        /// Он повторяет логику CanInteract, но без конкретного игрока-взаимодействующего.
        /// </summary>
        public bool HasAnyAvailableInteractor()
        {
            // Если стола нет или бот еще не дошел до стола, взаимодействовать нельзя.
            if (blackGregTable == null || !blackGregTable.IsBotReachedTable)
            {
                return false;
            }

            // Если в игровой зоне нет игроков, взаимодействовать некому.
            if (blackGregTable.playersInGameArea == null || blackGregTable.playersInGameArea.Count == 0)
            {
                return false;
            }

            // Если стол занят, взаимодействовать может только владелец стола,
            // и только если он находится в игровой зоне.
            if (blackGregTable.IsOccupied)
            {
                return blackGregTable.playersInGameArea.Contains(blackGregTable.OccupiedByClientId);
            }

            // Если стол не занят, любой игрок из игровой зоны может взаимодействовать.
            return true;
        }
    }
}