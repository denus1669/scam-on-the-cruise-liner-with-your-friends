using Blocks.Gameplay.Core;
using UnityEngine;

namespace Assets.Casino.Games.BlackGreg
{
    /// <summary>
    /// Подсветка колоды карт.
    ///
    /// Отвечает только за правила доступности конкретной колоды.
    /// Визуал и сетевая синхронизация находятся в базовом классе.
    /// </summary>
    [RequireComponent(typeof(DeckInteractable))]
    public class DeckHighlighter : InteractableHighlighterBase
    {
        [Header("Dependencies")]
        [Tooltip("Интерактивная колода, правила которой используются для подсветки.")]
        [SerializeField] private DeckInteractable deckInteractable;

        [Header("Availability Settings")]
        [Tooltip("Интервал проверки доступности колоды в секундах.")]
        [SerializeField, Min(0.05f)] private float availabilityCheckInterval = 0.25f;

        private float _checkTimer;

        private void Reset()
        {
            // Автоматически подтягиваем ссылку в редакторе.
            deckInteractable = GetComponent<DeckInteractable>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (deckInteractable == null)
            {
                deckInteractable = GetComponent<DeckInteractable>();
            }

            // Сразу проверяем доступность после спавна.
            if (IsServer)
            {
                RefreshAvailability();
            }
        }

        private void Update()
        {
            // Проверка доступности выполняется только на сервере,
            // потому что подсветка доступности видна всем клиентам.
            if (!IsSpawned || !IsServer)
                return;

            _checkTimer += Time.deltaTime;

            if (_checkTimer < availabilityCheckInterval)
                return;

            _checkTimer = 0f;

            RefreshAvailability();
        }

        /// <summary>
        /// Обновляет состояние доступности колоды.
        ///
        /// Использует отдельный метод колоды, который повторяет логику CanInteract,
        /// но отвечает на вопрос:
        /// "Может ли хотя бы один игрок сейчас взаимодействовать с этой колодой?"
        /// </summary>
        private void RefreshAvailability()
        {
            if (deckInteractable == null)
            {
                SetAvailableHighlight(false);
                return;
            }

            bool available = deckInteractable.HasAnyAvailableInteractor();

            SetAvailableHighlight(available);
        }
    }
}