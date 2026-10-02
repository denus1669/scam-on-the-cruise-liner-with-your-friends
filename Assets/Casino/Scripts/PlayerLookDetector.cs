using Blocks.Gameplay.Core;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Bot
{
    /// <summary>
    /// Отслеживает, смотрит ли игрок на этот объект.
    /// Вешается на бота. Пишет в консоль зеленым если смотрят, красным - если нет.
    /// Использует BotVision для проверки "видит ли бот игрока".
    /// </summary>
    [RequireComponent(typeof(BotVision))]
    public class PlayerLookDetector : NetworkBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] BaseBotBehaviour baseBotBehaviour;
        [SerializeField] private BotVision botVision;
        [SerializeField] private BotAgent botAgent;

        [Header("Настройки")]
        [Tooltip("Угол (в градусах), в пределах которого взгляд игрока считается направленным на объект")]
        [Range(5f, 90f)]
        [SerializeField] private float lookAngleThreshold = 30f;

        [Tooltip("Максимальный угол отклонения взгляда по вертикали (в градусах)")]
        [Range(5f, 90f)]
        [SerializeField] private float maxPitchAngle = 45f;

        [Tooltip("Максимальная дистанция обнаружения")]
        [SerializeField] private float maxDistance = 20f;

        [Tooltip("Интервал проверки (секунды)")]
        [SerializeField] private float checkInterval = 0.25f;

        [Tooltip("Слой игроков")]
        [SerializeField] private LayerMask targetLayer; // "Player"

        [Tooltip("Точка, на которую должен быть направлен взгляд (если пусто — используется transform)")]
        [SerializeField] private Transform lookTarget;

        [Tooltip("Использовать камеру игрока вместо его forward (если есть)")]
        [SerializeField] private bool usePlayerCamera = true;

        [Header("Проверка по высоте (Y)")]
        [Tooltip("Включить ограничение по вертикали — игрок должен смотреть примерно на одной высоте с целью")]
        [SerializeField] private bool useHeightCheck = true;

        [Tooltip("Максимальное отклонение по высоте (в метрах) между взглядом игрока и целью")]
        [SerializeField] private float maxHeightDifference = 3f;

        [Tooltip("Учитывать направление взгляда по вертикали (pitch). Если выключено — проверяется только разница высот позиций")]
        [SerializeField] private bool checkViewPitch = true;

        [Header("Отладка")]
        [SerializeField] private List<NetworkObject> playersLookAtMe = new();

        // Буфер для NonAlloc. 32 хватит с запасом; при переполнении — увеличьте.
        private readonly Collider[] _overlapBuffer = new Collider[32];
        private float _timer;
        private bool _isBeingWatched;
        private bool _isStartTrack = false;

        /// <summary>Текущее состояние наблюдения.</summary>
        private PlayerWatchState _watchState = PlayerWatchState.Absent;

        /// <summary>Смотрит ли сейчас хотя бы один игрок на объект.</summary>
        public bool IsBeingWatched => _isBeingWatched;

        /// <summary>Текущее состояние наблюдения (Absent / Present / Watching).</summary>
        public PlayerWatchState WatchState => _watchState;

        /// <summary>Список игроков, которые сейчас смотрят.</summary>
        public IReadOnlyList<NetworkObject> PlayersLookAtMe => playersLookAtMe;

        private Transform LookTarget => lookTarget != null ? lookTarget : transform;

        public void SetIsStartTrack(bool isStartTrack) =>
            _isStartTrack = isStartTrack;

        public bool GetIsStartTrack()
        {
            return _isStartTrack;
        }

        /// <summary>
        /// Возвращает текущее состояние наблюдения.
        /// Absent   — игроков рядом нет;
        /// Present  — игроки рядом, но не смотрят;
        /// Watching — хотя бы один игрок смотрит.
        /// </summary>
        public PlayerWatchState GetPlayerWatchState() => _watchState;

        private void Awake()
        {
            botVision = GetComponent<BotVision>();
            botAgent = GetComponent<BotAgent>();
            RebindToActiveBehaviour();
        }

        private void Update()
        {
            if (!IsServer) return;

            if (baseBotBehaviour == null || !baseBotBehaviour.enabled)
                RebindToActiveBehaviour();

            if (baseBotBehaviour == null || !baseBotBehaviour.IsPlaying) return;

            if (botAgent == null || !(botAgent.CurrentTable is PlayerGameTable)) return;

            _timer += Time.deltaTime;
            if (_timer < checkInterval) return;
            _timer = 0f;

            EvaluateLook();
        }

        private void RebindToActiveBehaviour()
        {
            var behaviours = GetComponents<BaseBotBehaviour>();
            foreach (var b in behaviours)
            {
                if (b != null && b.enabled)
                {
                    baseBotBehaviour = b;
                    return;
                }
            }
        }

        private void EvaluateLook()
        {
            playersLookAtMe.Clear();
            _isBeingWatched = false;

            Vector3 targetPos = LookTarget.position;

            int count = Physics.OverlapSphereNonAlloc(
                targetPos,
                maxDistance,
                _overlapBuffer,
                targetLayer,
                QueryTriggerInteraction.Ignore);

            if (count < 1)
            {
                _watchState = PlayerWatchState.Absent;
                LogState();
                return;
            }

            // Есть ли рядом хотя бы один валидный игрок (NetworkObject)
            bool anyPlayerNearby = false;

            for (int i = 0; i < count; i++)
            {
                var col = _overlapBuffer[i];
                if (col == null) continue;

                // Коллайдер может быть на дочернем объекте — ищем NetworkObject вверх по иерархии
                if (!col.TryGetComponent(out NetworkObject netObj))
                    netObj = col.GetComponentInParent<NetworkObject>();

                if (netObj == null) continue;

                anyPlayerNearby = true;

                if (playersLookAtMe.Contains(netObj)) continue; // защита от дублей (несколько коллайдеров)

                // --- BotVision: бот должен видеть игрока, чтобы проверить его взгляд ---
                // Если бот не видит игрока — игрок всё равно даёт Present (через anyPlayerNearby),
                // но Watching мы проверить не можем (бот не знает, смотрит ли на него тот, кого он не видит).
                if (botVision != null && !botVision.CanSeePoint(netObj.transform.position))
                    continue;

                if (IsPlayerLookingAt(netObj, targetPos))
                {
                    playersLookAtMe.Add(netObj);
                    _isBeingWatched = true;
                }
            }

            // Определяем итоговое состояние
            if (_isBeingWatched)
                _watchState = PlayerWatchState.Watching;
            else if (anyPlayerNearby)
                _watchState = PlayerWatchState.Present;
            else
                _watchState = PlayerWatchState.Absent;

            LogState();
        }

        private bool IsPlayerLookingAt(NetworkObject player, Vector3 targetPos)
        {
            Transform view = FindViewTransform(player);

            if (view == null)
            {
                return false;
            }

            Vector3 toTarget = targetPos - view.position;
            float distance = toTarget.magnitude;

            if (distance > maxDistance)
            {
                return false;
            }
            if (distance < 0.0001f) return true;

            toTarget /= distance;

            // --- Проверка по высоте ---
            if (useHeightCheck)
            {
                float heightDiff = Mathf.Abs(view.position.y - targetPos.y);
                if (heightDiff > maxHeightDifference)
                {
                    return false;
                }

                if (checkViewPitch)
                {
                    float viewPitch = Mathf.Asin(Mathf.Clamp(view.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
                    if (Mathf.Abs(viewPitch) > maxPitchAngle)
                    {
                        return false;
                    }
                }
            }

            float cosThreshold = Mathf.Cos(lookAngleThreshold * Mathf.Deg2Rad);
            float dot = Vector3.Dot(view.forward, toTarget);
            if (dot < cosThreshold)
            {
                float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
                return false;
            }

            // Raycast
            if (Physics.Raycast(view.position, toTarget, out RaycastHit hit, distance))
            {
                // Луч достиг любой части бота (корень или дочерний) — препятствий нет
                bool hitTheBot = (hit.transform == transform) || hit.transform.IsChildOf(transform);
                if (!hitTheBot)
                    return false;
            }

            return true;
        }

        private Transform FindViewTransform(NetworkObject player)
        {
            // 1. Если есть камера — берём её (на случай, если она всё-таки дочерняя)
            if (usePlayerCamera)
            {
                var cam = player.GetComponentInChildren<Camera>();
                if (cam != null) return cam.transform;
            }

            // 2. Ищем PlayerCameraRoot по тегу CinemachineTarget
            var camRoots = player.GetComponentsInChildren<Transform>(true);
            foreach (var t in camRoots)
            {
                if (t.CompareTag("CinemachineTarget"))
                    return t;
            }

            // 3. Фолбэк — корень игрока
            return player.transform;
        }

        private void LogState()
        {
            /*
            switch (_watchState)
            {
                case PlayerWatchState.Watching:
                    Debug.Log($"<color=green>[LookDetector] Watching: на меня смотрят ({playersLookAtMe.Count})</color>", this);
                    break;
                case PlayerWatchState.Present:
                    Debug.Log("<color=yellow>[LookDetector] Present: игроки рядом, но не смотрят</color>", this);
                    break;
                case PlayerWatchState.Absent:
                    Debug.Log("<color=red>[LookDetector] Absent: игроков рядом нет</color>", this);
                    break;
            }*/
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 pos = LookTarget != null ? LookTarget.position : transform.position;

            // Радиус обнаружения
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Gizmos.DrawWireSphere(pos, maxDistance);

            // Конус взгляда (относительно forward бота — просто для наглядности порога)
            Gizmos.color = Color.yellow;
            Vector3 leftDir = Quaternion.Euler(0f, -lookAngleThreshold, 0f) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0f, lookAngleThreshold, 0f) * transform.forward;
            Gizmos.DrawRay(pos, leftDir * maxDistance);
            Gizmos.DrawRay(pos, rightDir * maxDistance);

            // Визуализация зоны по высоте
            if (useHeightCheck)
            {
                Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);
                Vector3 bottom = pos + Vector3.down * maxHeightDifference;
                Vector3 top = pos + Vector3.up * maxHeightDifference;
                Gizmos.DrawLine(bottom, top);
                Gizmos.DrawWireSphere(bottom, 0.2f);
                Gizmos.DrawWireSphere(top, 0.2f);

                // Конус наклона взгляда по вертикали (pitch)
                if (checkViewPitch)
                {
                    Gizmos.color = Color.cyan;

                    Vector3 upDir = Quaternion.Euler(-maxPitchAngle, 0f, 0f) * transform.forward;
                    Vector3 downDir = Quaternion.Euler(maxPitchAngle, 0f, 0f) * transform.forward;

                    Gizmos.DrawRay(pos, upDir * maxDistance);
                    Gizmos.DrawRay(pos, downDir * maxDistance);
                }
            }
        }
#endif
    }
}