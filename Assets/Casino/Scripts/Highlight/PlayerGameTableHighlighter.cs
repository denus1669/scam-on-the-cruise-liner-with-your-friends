using Assets.Casino.QuickOutline.Scripts;
using Blocks.Gameplay.Core;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Games
{
    /// <summary>
    /// Визуальный компонент подсветки для PlayerGameTable.
    /// Локально вычисляет доступность на основе NetworkVariable стола.
    /// НЕ использует RPC — все данные уже синхронизированы.
    /// </summary>
    public class PlayerGameTableHighlighter : MonoBehaviour
    {
        [SerializeField] private PlayerGameTable _playerTable;
        protected Outline _outline;
        protected bool _available;

        private void Reset()
        {
            _playerTable = GetComponent<PlayerGameTable>();
            if (_playerTable == null) _playerTable = GetComponentInParent<PlayerGameTable>();
        }

        protected virtual void Awake()
        {
            if (_outline == null)
            {
                _outline = GetComponent<Outline>();
            }
            if (_playerTable == null)
            {
                _playerTable = GetComponent<PlayerGameTable>();
                if (_playerTable == null) _playerTable = GetComponentInParent<PlayerGameTable>();
            }
            SetAvailableHighlight(false);
        }

        private void OnEnable()
        {
            if (_playerTable == null)
            {
                Debug.LogError("[PlayerGameTableHighlighter] PlayerGameTable не найден!");
                return;
            }

            // Подписываемся на все события, которые влияют на доступность
            _playerTable.OnBotReachedTableStateChanged += HandleStateChanged;
            _playerTable.OnGameStateChanged += HandleStateChanged;
            _playerTable.playersInGameArea.OnListChanged += HandlePlayersListChanged;

            // Синхронизируем начальное состояние (важно для Late Joiners)
            EvaluateAvailability();
        }

        private void OnDisable()
        {
            if (_playerTable != null)
            {
                _playerTable.OnBotReachedTableStateChanged -= HandleStateChanged;
                _playerTable.OnGameStateChanged -= HandleStateChanged;
                _playerTable.playersInGameArea.OnListChanged -= HandlePlayersListChanged;
            }
            else Debug.LogWarning($"[PlayerGameTableHighlither] _playerTable == null");
        }

        private void HandleStateChanged(bool _)
        {
            Debug.Log($"[PlayerGameTableHighlither] HandleStateChanged");
            EvaluateAvailability();
        }

        private void HandlePlayersListChanged(NetworkListEvent<ulong> change)
        {
            Debug.Log($"[PlayerGameTableHighlither] HandlePlayersListChanged");

            EvaluateAvailability();
        }

        /// <summary>
        /// Локальное вычисление доступности на основе NetworkVariable.
        /// Каждый клиент вычисляет это для себя, без RPC.
        /// </summary>
        private void EvaluateAvailability()
        {
            if (_playerTable == null)
            {
                Debug.LogWarning($"[PlayerGameTableHighlither] _playerTable == null");
                return;
            }

            // Глобальные условия: бот на месте И игра не идет
            bool isTableAvailableGlobally = _playerTable.IsBotReachedTable && !_playerTable.IsGameStarted;
            Debug.Log($"[PlayerGameTableHighlither] _isTableAvailableGlobally {isTableAvailableGlobally} + _playerTable.IsBotReachedTable {_playerTable.IsBotReachedTable} + !_playerTable.IsGameStarted {!_playerTable.IsGameStarted}");

            // Локальная проверка: этот клиент в зоне?
            bool isInArea = false;
            if (NetworkManager.Singleton != null)
            {
                isInArea = _playerTable.playersInGameArea.Contains(NetworkManager.Singleton.LocalClientId);
            }

            // Подсветка видна, если стол доступен глобально И игрок НЕ в зоне
            bool shouldHighlight = isTableAvailableGlobally && !isInArea;
            Debug.Log($"[PlayerGameTableHighlither] shouldHighlight {shouldHighlight} +  isTableAvailableGlobally {isTableAvailableGlobally} + !isInArea {!isInArea}");

            // Применяем к визуалу (метод из HighlightControllerBase)
            SetAvailableHighlight(shouldHighlight);
        }

        public virtual void SetAvailableHighlight(bool state)
        {
            Debug.Log($"state {state}");

            _available = state;
            _outline.enabled = state;
            
        }
    }
}