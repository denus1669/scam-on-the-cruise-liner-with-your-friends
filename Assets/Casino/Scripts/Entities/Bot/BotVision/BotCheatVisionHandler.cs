using Assets.Casino.Cheating;
using Assets.Casino.Games;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Bot
{
    /// <summary>
    /// Наблюдатель: игрок читерит и бот это видит → начисляет раздражение.
    /// Отвечает только за:
    /// 1. Периодическую проверку "читерит ли игрок + видит ли его бот".
    /// 2. Начисление раздражения через BotDispleasureController.
    /// 3. Отладочную визуализацию (линия + лог).
    /// Не отвечает за логику самого читерства (это CheatController)
    /// и за реакцию бота (это OnCheatSpotted — заглушка).
    /// </summary>
    [RequireComponent(typeof(BotVision))]
    [RequireComponent(typeof(BotDispleasureController))]
    [RequireComponent(typeof(BotAgent))]
    [RequireComponent(typeof(BaseBotBehaviour))]
    public class BotCheatVisionHandler : NetworkBehaviour
    {
        [Header("Настройки")]
        [Tooltip("Раздражение в секунду, когда бот видит мухлеж")]
        [SerializeField] private float cheatDispleasurePerSecond = 10f;
        [Tooltip("Интервал проверки (секунды)")]
        [SerializeField] private float checkInterval = 0.25f;

        [Header("Ссылки (кэшируются в Awake)")]
        [SerializeField] private BotVision _botVision;
        [SerializeField] private BotDispleasureController _displeasureController;
        [SerializeField] private BotAgent _botAgent;
        private BaseBotBehaviour _botBehaviour; // Ищем динамически — на боте несколько behavior'ов

        [Header("Отладка")]
        [Tooltip("Рисовать линию от бота к игроку в рантайме")]
        [SerializeField] private bool drawDebugLine = true;
        [Tooltip("Логировать смену состояния 'видит читающего игрока'")]
        [SerializeField] private bool logVisionChanges = true;
        [Tooltip("Высота старта линии от бота (примерно уровень глаз)")]
        [SerializeField] private float debugLineBotHeight = 1.7f;
        [Tooltip("Высота конца линии у игрока")]
        [SerializeField] private float debugLinePlayerHeight = 1.5f;

        private float _timer;
        private bool _isSeeingCheat;
        private bool _lastSeeingCheat;

        // Кэш контроллера игрока, чтобы не искать его каждый цикл
        private CheatController _cachedPlayerCheatController;
        private ulong _cachedPlayerClientId = ulong.MaxValue;

        private void Awake()
        {
            _botVision = GetComponent<BotVision>();
            _displeasureController = GetComponent<BotDispleasureController>();
            _botAgent = GetComponent<BotAgent>();
            RebindToActiveBehaviour();
        }

        private void Update()
        {
            if (!IsServer) return;

            // Если текущий behavior неактивен — ищем новый активный
            // (BotAgent переключает behavior при смене стола)
            if (_botBehaviour == null || !_botBehaviour.enabled)
            {
                RebindToActiveBehaviour();
            }

            // Пропускаем, если бот не в активной сессии
            if (_botBehaviour == null || !_botBehaviour.IsPlaying) return;

            // Проверяем только за игровыми столами (PlayerGameTable).
            // У слотов, баров и в коридорах боту не нужно детектировать читерство.
            if (_botAgent == null || !(_botAgent.CurrentTable is PlayerGameTable)) return;

            ProcessCheatDetection();
            ProcessCheatDispleasure();
        }

        /// <summary>
        /// Ищет активный BaseBotBehaviour среди всех на объекте.
        /// BotAgent отключает все behavior'ы кроме совместимого с текущим столом.
        /// </summary>
        private void RebindToActiveBehaviour()
        {
            var behaviours = GetComponents<BaseBotBehaviour>();
            foreach (var b in behaviours)
            {
                if (b != null && b.enabled)
                {
                    _botBehaviour = b;
                    return;
                }
            }

            // Fallback: берём первый, если нет активных (до старта игры)
            if (behaviours.Length > 0)
            {
                _botBehaviour = behaviours[0];
            }
        }

        /// <summary>
        /// Периодически проверяет: читерит ли игрок И видит ли его бот.
        /// При первом обнаружении вызывает OnCheatSpotted (заглушка для реакции).
        /// </summary>
        private void ProcessCheatDetection()
        {
            _timer += Time.deltaTime;
            if (_timer < checkInterval) return;
            _timer = 0f;

            bool wasSeeing = _isSeeingCheat;
            _isSeeingCheat = CheckPlayerCheatVisibility();

            // Отладка: линия от бота к игроку (всегда, независимо от IsCheating)
            if (drawDebugLine)
                DrawDebugVisionLine();

            // Отладка: лог при смене состояния
            if (logVisionChanges && _isSeeingCheat != wasSeeing)
            {
                Debug.Log($"[BotCheatVision] Бот '{gameObject.name}' → игрок: " +
                          $"{(_isSeeingCheat ? "<color=green>ВИДИТ ЧИТ</color>" : "<color=red>НЕ ВИДИТ ЧИТ</color>")}");
                _lastSeeingCheat = _isSeeingCheat;
            }

            // Первый кадр обнаружения — уведомляем о поимке
            if (_isSeeingCheat && !wasSeeing)
                OnCheatSpotted(_cachedPlayerClientId);
        }

        /// <summary>
        /// Непрерывно начисляет раздражение, пока бот видит мухлеж.
        /// </summary>
        private void ProcessCheatDispleasure()
        {
            if (!_isSeeingCheat) return;
            float amount = cheatDispleasurePerSecond * Time.deltaTime;
            _displeasureController.AddInstantDispleasure(amount);
        }

        /// <summary>
        /// Проверка: игрок читерит И бот его видит (через BotVision).
        /// </summary>
        private bool CheckPlayerCheatVisibility()
        {
            CheatController cheatController = GetPlayerCheatController();
            if (cheatController == null || !cheatController.IsCheating)
                return false;

            NetworkObject playerNetObj = cheatController.NetworkObject;
            if (playerNetObj == null) return false;

            return _botVision.CanSeePoint(playerNetObj.transform.position);
        }

        /// <summary>
        /// Рисует отладочную линию от бота к игроку с 3-цветной кодировкой:
        /// Зелёный — бот видит читающего игрока
        /// Жёлтый — бот видит игрока, но тот не читерит
        /// Красный — бот не видит игрока
        /// </summary>
        private void DrawDebugVisionLine()
        {
            IGameTable table = _botAgent.CurrentTable;
            if (table == null || !table.IsOccupied) return;

            if (NetworkManager.Singleton?.SpawnManager == null) return;

            NetworkObject playerNetObj = NetworkManager.Singleton.SpawnManager
                .GetPlayerNetworkObject(table.OccupiedByClientId);

            if (playerNetObj == null) return;

            bool canSee = _botVision != null && _botVision.CanSeePoint(playerNetObj.transform.position);
            bool isCheating = _cachedPlayerCheatController != null && _cachedPlayerCheatController.IsCheating;

            Vector3 from = transform.position + Vector3.up * debugLineBotHeight;
            Vector3 to = playerNetObj.transform.position + Vector3.up * debugLinePlayerHeight;

            Color c = !canSee ? Color.red : (isCheating ? Color.green : Color.yellow);
            Debug.DrawLine(from, to, c, checkInterval);
        }

        /// <summary>
        /// Возвращает CheatController игрока за столом (с кэшированием по ClientId).
        /// </summary>
        private CheatController GetPlayerCheatController()
        {
            if (NetworkManager.Singleton?.SpawnManager == null)
                return null;

            IGameTable table = _botAgent.CurrentTable;
            if (table == null || !table.IsOccupied)
            {
                InvalidateCache();
                return null;
            }

            ulong playerClientId = table.OccupiedByClientId;

            // Кэш: игрок не изменился — возвращаем закэшированный контроллер
            if (_cachedPlayerClientId == playerClientId && _cachedPlayerCheatController != null)
                return _cachedPlayerCheatController;

            _cachedPlayerClientId = playerClientId;
            NetworkObject playerNetObj = NetworkManager.Singleton.SpawnManager
                .GetPlayerNetworkObject(playerClientId);

            _cachedPlayerCheatController = playerNetObj != null
                ? playerNetObj.GetComponent<CheatController>()
                : null;

            return _cachedPlayerCheatController;
        }

        private void InvalidateCache()
        {
            _cachedPlayerCheatController = null;
            _cachedPlayerClientId = ulong.MaxValue;
        }

        /// <summary>
        /// Заглушка для будущей реакции бота на обнаруженный мухлеж.
        /// Сюда подключается анимация поворота головы, звук тревоги и т.д.
        /// </summary>
        private void OnCheatSpotted(ulong cheaterClientId)
        {
            Debug.Log($"[BotCheatVision] Бот {gameObject.name} заметил мухлеж игрока {cheaterClientId}");
            // TODO: анимация, звук, поворот головы — отдельная задача
        }
    }
}