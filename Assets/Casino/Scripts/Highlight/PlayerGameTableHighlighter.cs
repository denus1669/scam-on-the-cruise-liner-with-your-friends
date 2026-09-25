using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Games
{
    /// <summary>
    /// Визуальный компонент подсветки для PlayerGameTable.
    /// Локально вычисляет доступность на основе NetworkVariable стола.
    /// НЕ использует RPC — все данные уже синхронизированы.
    /// </summary>
    public class PlayerGameTableHighlighter : HighlightControllerBase
    {
        [SerializeField] private PlayerGameTable _playerTable;

        private void Reset()
        {
            _playerTable = GetComponent<PlayerGameTable>();
            if (_playerTable == null) _playerTable = GetComponentInParent<PlayerGameTable>();
        }

        private void OnEnable()
        {
            if (_playerTable == null)
            {
                _playerTable = GetComponent<PlayerGameTable>();
                if (_playerTable == null) _playerTable = GetComponentInParent<PlayerGameTable>();
            }

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
        }

        private void HandleStateChanged(bool _) => EvaluateAvailability();
        private void HandlePlayersListChanged(NetworkListEvent<ulong> change) => EvaluateAvailability();

        /// <summary>
        /// Локальное вычисление доступности на основе NetworkVariable.
        /// Каждый клиент вычисляет это для себя, без RPC.
        /// </summary>
        private void EvaluateAvailability()
        {
            if (_playerTable == null) return;

            // Глобальные условия: бот на месте И игра не идет
            bool isTableAvailableGlobally = _playerTable.IsBotReachedTable && !_playerTable.IsGameStarted;

            // Локальная проверка: этот клиент в зоне?
            bool isInArea = false;
            if (NetworkManager.Singleton != null)
            {
                isInArea = _playerTable.playersInGameArea.Contains(NetworkManager.Singleton.LocalClientId);
            }

            // Подсветка видна, если стол доступен глобально И игрок НЕ в зоне
            bool shouldHighlight = isTableAvailableGlobally && !isInArea;

            // Применяем к визуалу (метод из HighlightControllerBase)
            SetAvailableHighlight(shouldHighlight);
        }
    }
}