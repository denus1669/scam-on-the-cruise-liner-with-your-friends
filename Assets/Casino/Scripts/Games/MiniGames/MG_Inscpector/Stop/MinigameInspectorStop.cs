using System;
using Assets.Casino.Cheating.MiniGames;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Casino.Scripts.Actions.Cheating.MiniGames
{
    [System.Flags]
    public enum BurstType
    {
        None         = 0,
        ForwardDown  = 1 << 0,
        ForwardUp    = 1 << 1,
        ForwardLeft  = 1 << 2,
        ForwardRight = 1 << 3,
        Down         = 1 << 4,
        Up           = 1 << 5,
        Left         = 1 << 6,
        Right        = 1 << 7,
    }

    public class MinigameInspectorStop : MinigameBase
    {
        public override MinigameType MinigameKind => MinigameType.Inspector;

        [Header("Inspector")]
        [SerializeField] private GameObject _inspectorGameObject;
        [SerializeField] private Transform inspectorSpawnPoint;
        [SerializeField] private float _baseSpeed = 10f;
        [SerializeField] private float _maxSpeed = 40f;
        [SerializeField] private float _speedGrowthPerSecond = 1.15f;

        [Header("Bursts (резкие рывки)")]
        [SerializeField] private Vector2 _burstIntervalRange = new Vector2(0.8f, 2.0f);
        [SerializeField] private Vector2 _burstDurationRange = new Vector2(0.08f, 0.2f);
        [SerializeField] private Vector2 _burstMultiplierRange = new Vector2(2.5f, 5.0f);

        [Tooltip("Какие направления разрешены для рывков. Forward* — относительно направления к выходу.")]
        [SerializeField] private BurstType _burstDirections =
            BurstType.ForwardDown | BurstType.ForwardRight | BurstType.Down | BurstType.Up;

        [Header("Player")]
        [SerializeField] private GameObject _playerGameObject;
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private float _playerSpeed = 15f;

        [Header("MiniGameSettings")]
        [SerializeField] private GameObject _exitPoint;
        [SerializeField] private TextMeshProUGUI _holdingTimeTMP;

        private Rigidbody2D _inspectorRb;
        private Rigidbody2D _playerRb;

        private float _holdingTime;
        private float _currentSpeed;
        private float _activeBurstMultiplier = 1f;
        private float _burstEndTime;
        private float _nextBurstTime;
        private Vector2 _burstDir = Vector2.right;

        private BurstType[] _enabledBursts;

        private void Awake()
        {
            _inspectorRb = _inspectorGameObject.GetComponent<Rigidbody2D>();
            _playerRb = _playerGameObject.GetComponent<Rigidbody2D>();
            if (_inspectorRb == null) _inspectorRb = _inspectorGameObject.AddComponent<Rigidbody2D>();
            if (_playerRb == null) _playerRb = _playerGameObject.AddComponent<Rigidbody2D>();

            ConfigureBody(_inspectorRb);
            ConfigureBody(_playerRb);

            EnsureCollider(_inspectorGameObject, new Vector2(100, 200));
            EnsureCollider(_playerGameObject, new Vector2(100, 200));

            BuildBurstCache();
        }

        private void ConfigureBody(Rigidbody2D rb)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.mass = 1f;
        }

        private void EnsureCollider(GameObject go, Vector2 size)
        {
            var col = go.GetComponent<BoxCollider2D>();
            if (col == null) col = go.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        private void BuildBurstCache()
        {
            var list = new System.Collections.Generic.List<BurstType>(8);
            foreach (BurstType t in Enum.GetValues(typeof(BurstType)))
                if (t != BurstType.None && (_burstDirections & t) != 0)
                    list.Add(t);
            _enabledBursts = list.ToArray();
        }

        protected override void GameInitializer(bool _isPlaying)
        {
            _holdingTime = 0;
            _currentSpeed = _baseSpeed;
            _activeBurstMultiplier = 1f;
            _burstEndTime = 0f;

            if (_isPlaying)
            {
                _inspectorGameObject.transform.localPosition = inspectorSpawnPoint.localPosition;
                _playerGameObject.transform.localPosition = playerSpawnPoint.localPosition;

                _inspectorGameObject.SetActive(true);
                _playerGameObject.SetActive(true);
                _holdingTimeTMP.gameObject.SetActive(true);

                _nextBurstTime = Time.time + UnityEngine.Random.Range(_burstIntervalRange.x, _burstIntervalRange.y);
            }
            else
            {
                if (_inspectorRb != null) _inspectorRb.linearVelocity = Vector2.zero;
                if (_playerRb != null) _playerRb.linearVelocity = Vector2.zero;

                _inspectorGameObject.SetActive(false);
                _playerGameObject.SetActive(false);
                _holdingTimeTMP.gameObject.SetActive(false);
            }
        }

        private void FixedUpdate()
        {
            if (!_isPlaying) return;
            GameProcess();
        }

        private void Update()
        {
            if (!_isPlaying) return;
            _holdingTime += Time.deltaTime;
            _holdingTimeTMP.SetText("Время удержания: {0:F2}", _holdingTime);
        }

        protected override void GameProcess()
        {
            PlayerGameActions();
            InspectorGameActions();
        }

        protected override void PlayerGameActions()
        {
            Vector2 move = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) move += Vector2.up;
            if (Keyboard.current.sKey.isPressed) move += Vector2.down;
            if (Keyboard.current.aKey.isPressed) move += Vector2.left;
            if (Keyboard.current.dKey.isPressed) move += Vector2.right;

            _playerRb.linearVelocity = move.normalized * _playerSpeed;
        }

        private void InspectorGameActions()
        {
            if (_exitPoint == null) return;

            Vector2 pos = _inspectorRb.position;
            Vector2 toExit = (Vector2)_exitPoint.transform.position - pos;
            Vector2 exitDir = toExit.sqrMagnitude > 0.0001f ? toExit.normalized : Vector2.right;

            UpdateBursts(exitDir);

            bool bursting = Time.time < _burstEndTime;
            Vector2 dir = bursting ? _burstDir : exitDir;

            float speed = Mathf.Min(_currentSpeed * _activeBurstMultiplier, _maxSpeed);
            _inspectorRb.linearVelocity = dir * speed;

            _currentSpeed = Mathf.Min(
                _currentSpeed * Mathf.Pow(_speedGrowthPerSecond, Time.fixedDeltaTime),
                _maxSpeed
            );
        }

        private void UpdateBursts(Vector2 exitDir)
        {
            if (Time.time >= _burstEndTime)
                _activeBurstMultiplier = 1f;

            if (Time.time >= _nextBurstTime)
                StartBurst(exitDir);
        }

        private void StartBurst(Vector2 exitDir)
        {
            _activeBurstMultiplier = UnityEngine.Random.Range(_burstMultiplierRange.x, _burstMultiplierRange.y);
            _burstEndTime = Time.time + UnityEngine.Random.Range(_burstDurationRange.x, _burstDurationRange.y);
            _nextBurstTime = Time.time + UnityEngine.Random.Range(_burstIntervalRange.x, _burstIntervalRange.y);

            if (_enabledBursts.Length > 0)
            {
                var pick = _enabledBursts[UnityEngine.Random.Range(0, _enabledBursts.Length)];
                _burstDir = DirectionFor(pick, exitDir);
            }
            else
            {
                _burstDir = exitDir;
            }
        }

        private Vector2 DirectionFor(BurstType t, Vector2 exitDir)
        {
            Vector2 dir;
            switch (t)
            {
                case BurstType.ForwardDown:  dir = exitDir + Vector2.down;  break;
                case BurstType.ForwardUp:    dir = exitDir + Vector2.up;    break;
                case BurstType.ForwardLeft:  dir = exitDir + Vector2.left;  break;
                case BurstType.ForwardRight: dir = exitDir + Vector2.right; break;
                case BurstType.Down:         dir = Vector2.down;            break;
                case BurstType.Up:           dir = Vector2.up;              break;
                case BurstType.Left:         dir = Vector2.left;            break;
                case BurstType.Right:        dir = Vector2.right;           break;
                default:                     dir = exitDir;                 break;
            }
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : exitDir;
        }
    }
}