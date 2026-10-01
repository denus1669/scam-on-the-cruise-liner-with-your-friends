using UnityEngine;
using Blocks.Gameplay.Core;

namespace Assets.Casino.Bot
{
    /// <summary>
    /// Временный отладочный компонент для проверки BotVision.
    /// Вешается на бота. Логирует и рисует, видит ли бот локального игрока.
    /// Удалить после тестирования.
    /// </summary>
    public class BotVisionDebugger : MonoBehaviour
    {
        [SerializeField] private BotVision botVision;

        [SerializeField] private float checkInterval = 0.5f;

        private Transform _playerTransform;
        private bool _lastResult;
        private float _timer;

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < checkInterval) return;
            _timer = 0f;

            FindPlayerIfNeeded();
            if (_playerTransform == null || botVision == null) return;

            bool sees = botVision.CanSeePoint(_playerTransform.position);

            // Логируем только при изменении состояния
            if (sees != _lastResult)
            {
                Debug.Log($"[BotVisionDebugger] Бот '{gameObject.name}' → игрок: {(sees ? "<color=green>ВИДИТ</color>" : "<color=red>НЕ ВИДИТ</color>")}");
                _lastResult = sees;
            }

            // Рисуем линию в рантайме
            Color c = sees ? Color.green : Color.red;
            Debug.DrawLine(transform.position + Vector3.up * 2f, _playerTransform.position + Vector3.up * 1f, c, checkInterval);
        }

        private void FindPlayerIfNeeded()
        {
            if (_playerTransform != null) return;

            var playerManagers = FindObjectsByType<CorePlayerManager>(FindObjectsSortMode.None);
            foreach (var pm in playerManagers)
            {
                if (pm.IsOwner)
                {
                    _playerTransform = pm.transform;
                    return;
                }
            }
        }
    }
}