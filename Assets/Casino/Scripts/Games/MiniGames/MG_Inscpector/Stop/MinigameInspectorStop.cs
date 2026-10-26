using System;
using Assets.Casino.Cheating.MiniGames;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Casino.Scripts.Actions.Cheating.MiniGames
{
    public class MinigameInspectorStop : MinigameBase
    {
        [Header("Inspector")]
        [SerializeField] private GameObject _inspectorGameObject;
        [SerializeField] private float _baseInspectorSpeed = 5f;
        [SerializeField] private float _currentInspectorSpeed;
        [SerializeField] private float _multiplierInspectorSpeed = 1.2f;
        [Header("Player")]
        [SerializeField] private GameObject _playerGameObject;
        [SerializeField] private float _playerSpeed = 10f;

        [Header("MiniGameSettings")]
        [SerializeField] private GameObject _exitPoint;
        [SerializeField] private TextMeshProUGUI _holdingTimeTMP;

        private Rigidbody2D _inspectorRb;
        private Rigidbody2D _playerRb;

        private float _holdingTime = 0;
        
        [Header("Inspector Evasion")]
        [SerializeField] private float _maxInspectorSpeed = 35f;
        [SerializeField] private float _forwardWeight = 1.25f;
        [SerializeField] private float _steering = 18f;

        [SerializeField] private float _avoidRadius = 250f;
        [SerializeField] private float _avoidWeight = 2.5f;

        [SerializeField] private Vector2 _randomInterval = new Vector2(0.15f, 0.6f);
        [SerializeField] private Vector2 _sideWeightRange = new Vector2(0.35f, 1.15f);
        [SerializeField] private Vector2 _backwardWeightRange = new Vector2(0.05f, 0.35f);
        [SerializeField] private float _retreatChance = 0.22f;
        [SerializeField] private float _verticalJitter = 0.45f;

        [SerializeField] private Vector2 _burstInterval = new Vector2(0.6f, 1.6f);
        [SerializeField] private Vector2 _burstDuration = new Vector2(0.1f, 0.3f);
        [SerializeField] private Vector2 _burstMultiplierRange = new Vector2(1.6f, 3f);
        [SerializeField] private float _panicBurstChancePerSecond = 2.5f;

        private float _nextRandomTime;
        private float _nextBurstTime;
        private float _burstEndTime;
        private float _burstMultiplier = 1f;
        private Vector2 _evasionOffset;
        
        private void Awake()
        {
            if (_inspectorGameObject == null)
                Debug.LogError("[MinigameInspectorStop] inspectorGameObject is null");
            if (_playerGameObject == null)
                Debug.LogError("[MinigameInspectorStop] playerGameObject is null");

            // Кэшируем Rigidbody2D
            _inspectorRb = _inspectorGameObject.GetComponent<Rigidbody2D>();
            _playerRb = _playerGameObject.GetComponent<Rigidbody2D>();

            if (_inspectorRb == null)
                _inspectorRb = _inspectorGameObject.AddComponent<Rigidbody2D>();
            if (_playerRb == null)
                _playerRb = _playerGameObject.AddComponent<Rigidbody2D>();

            // Настраиваем тела: Dynamic, без гравитации, без вращения
            ConfigureBody(_inspectorRb);
            ConfigureBody(_playerRb);

            // Коллайдеры — размер под RectTransform (пример, подгоните под свои)
            var inspectorCol = _inspectorGameObject.GetComponent<BoxCollider2D>();
            if (inspectorCol == null) inspectorCol = _inspectorGameObject.AddComponent<BoxCollider2D>();
            inspectorCol.size = new Vector2(100, 200); // как у RectTransform

            var playerCol = _playerGameObject.GetComponent<BoxCollider2D>();
            if (playerCol == null) playerCol = _playerGameObject.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(100, 200);
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

        protected override void GameInitializer(bool _isPlaying)
        {
            _holdingTime = 0;
            _currentInspectorSpeed = _baseInspectorSpeed;

            if (_isPlaying)
            {
                ResetInspectorEvasion();

                _inspectorGameObject.SetActive(true);
                _playerGameObject.SetActive(true);
                _holdingTimeTMP.gameObject.SetActive(true);
            }
            else
            {
                if (_inspectorRb != null)
                    _inspectorRb.linearVelocity = Vector2.zero;

                if (_playerRb != null)
                    _playerRb.linearVelocity = Vector2.zero;

                _inspectorGameObject.SetActive(false);
                _playerGameObject.SetActive(false);
                _holdingTimeTMP.gameObject.SetActive(false);
            }
        }

        // Вся физика — в FixedUpdate
        private void FixedUpdate()
        {
            if (!_isPlaying) return;
            GameProcess();
        }

        // Update — только для UI (текст времени)
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

            // Двигаем через velocity — физика учитывает столкновения
            _playerRb.linearVelocity = move.normalized * _playerSpeed;
        }

        private void InspectorGameActions()
        {
            if (_exitPoint == null)
                return;

            Vector2 pos = _inspectorRb.position;
            Vector2 toExit = (Vector2)_exitPoint.transform.position - pos;
            Vector2 exitDir = toExit.sqrMagnitude > 0.0001f ? toExit.normalized : Vector2.right;

            UpdateInspectorEvasion(exitDir);

            // Основное направление всё ещё к выходу
            Vector2 dir = exitDir * _forwardWeight + _evasionOffset;

            // Уклонение от игрока
            Vector2 awayFromPlayer = pos - (Vector2)_playerRb.position;
            float playerDist = awayFromPlayer.magnitude;

            if (playerDist > 0.0001f && playerDist < _avoidRadius)
            {
                float pressure = 1f - playerDist / _avoidRadius;
                dir += awayFromPlayer.normalized * _avoidWeight * pressure;

                // Если игрок близко, инспектор может резко рвануть
                if (playerDist < _avoidRadius * 0.55f && Time.time >= _burstEndTime)
                {
                    if (UnityEngine.Random.value < _panicBurstChancePerSecond * Time.fixedDeltaTime)
                        StartBurst();
                }
            }

            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : exitDir;

            float speed = Mathf.Min(_currentInspectorSpeed * _burstMultiplier, _maxInspectorSpeed);
            Vector2 targetVelocity = dir * speed;

            // Движение через velocity, чтобы физика учитывала столкновения
            _inspectorRb.linearVelocity = Vector2.MoveTowards(
                _inspectorRb.linearVelocity,
                targetVelocity,
                _steering * Time.fixedDeltaTime
            );

            _currentInspectorSpeed = Mathf.Min(
                _currentInspectorSpeed * Mathf.Pow(_multiplierInspectorSpeed, Time.fixedDeltaTime),
                _maxInspectorSpeed
            );
        }
        
        private void ResetInspectorEvasion()
        {
            _burstMultiplier = 1f;
            _burstEndTime = 0f;
            _evasionOffset = Vector2.zero;

            _nextRandomTime = Time.time;
            _nextBurstTime = Time.time + UnityEngine.Random.Range(_burstInterval.x, _burstInterval.y);
        }

        private void UpdateInspectorEvasion(Vector2 exitDir)
        {
            if (Time.time >= _nextRandomTime)
            {
                Vector2 perp = new Vector2(-exitDir.y, exitDir.x);

                float side = UnityEngine.Random.Range(_sideWeightRange.x, _sideWeightRange.y);
                if (UnityEngine.Random.value < 0.5f)
                    side = -side;

                float back = UnityEngine.Random.value < _retreatChance
                    ? UnityEngine.Random.Range(_backwardWeightRange.x, _backwardWeightRange.y)
                    : UnityEngine.Random.Range(0f, _backwardWeightRange.x);

                float vertical = UnityEngine.Random.Range(-_verticalJitter, _verticalJitter);

                _evasionOffset = perp * side - exitDir * back + Vector2.up * vertical;
                _nextRandomTime = Time.time + UnityEngine.Random.Range(_randomInterval.x, _randomInterval.y);
            }

            if (Time.time >= _burstEndTime)
                _burstMultiplier = 1f;

            if (Time.time >= _nextBurstTime)
                StartBurst();
        }

        private void StartBurst()
        {
            _burstMultiplier = UnityEngine.Random.Range(_burstMultiplierRange.x, _burstMultiplierRange.y);
            _burstEndTime = Time.time + UnityEngine.Random.Range(_burstDuration.x, _burstDuration.y);
            _nextBurstTime = Time.time + UnityEngine.Random.Range(_burstInterval.x, _burstInterval.y);
        }
    }
}