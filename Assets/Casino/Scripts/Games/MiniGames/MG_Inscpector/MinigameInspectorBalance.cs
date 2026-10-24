using System;
using Assets.Casino.Cheating.MiniGames;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Casino.Scripts.Actions.Cheating.MiniGames
{
    /// <summary>
    /// Мини-игра «Баланс».
    /// Игрок держит Пробел — силуэт ускоряется вправо, ускорение линейно растёт от времени удержания.
    /// Отпустил — «гравитация» тянет влево, сила линейно растёт от времени покоя.
    /// Инспектор мечется по горизонтали, со временем ускоряясь и становясь непредсказуемее.
    /// Пока инспектор внутри силуэта — шкала успешности восстанавливается,
    /// снаружи — падает, причём тем быстрее, чем дольше длится игра.
    /// </summary>
    public class MinigameInspectorBalance : MinigameBase
    {
        [Header("Scene")]
        [SerializeField] private GameObject _inspectorGameObject;
        [SerializeField] private GameObject _playerGameObject;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI _timeTMP;
        [SerializeField] private TextMeshProUGUI _successTMP;
        [SerializeField] private Image _successFill;          // Image с Type = Filled, Horizontal
        [SerializeField] private Gradient _successGradient;   // красный -> зелёный (опционально)

        [Header("Play Area (горизонтальная зона)")]
        [Tooltip("Крайние точки зоны. Если не заданы — используются числа ниже.")]
        [SerializeField] private Transform _leftEdge;
        [SerializeField] private Transform _rightEdge;
        [SerializeField] private float _leftBoundX = -400f;
        [SerializeField] private float _rightBoundX = 400f;

        [Header("Player Balance")]
        [Tooltip("Половина ширины силуэта игрока — радиус «безопасной зоны».")]
        [SerializeField] private float _playerHalfWidth = 70f;
        [SerializeField] private float _holdAccelBase = 300f;
        [SerializeField] private float _holdAccelPerSecond = 1400f;   // линейный рост ускорения вправо
        [SerializeField] private float _gravityAccelBase = 200f;
        [SerializeField] private float _gravityAccelPerSecond = 1400f; // линейный рост «гравитации» влево
        [SerializeField] private float _maxAccel = 4000f;
        [SerializeField] private float _maxSpeed = 1800f;
        [SerializeField] private float _holdTimeCap = 2f;
        [SerializeField] private float _idleTimeCap = 2f;

        [Header("Inspector AI")]
        [SerializeField] private float _inspectorBaseSpeed = 150f;
        [SerializeField] private float _inspectorMaxSpeed = 900f;
        [Tooltip("Экспоненциальный рост скорости: speed = base * (1 + g)^t")]
        [SerializeField] private float _inspectorSpeedGrowthPerSecond = 0.05f;
        [SerializeField] private float _inspectorAccel = 1200f;
        [SerializeField] private float _inspectorArriveDistance = 10f;
        [SerializeField] private Vector2 _targetInterval = new Vector2(0.6f, 1.4f);
        [SerializeField] private Vector2 _burstInterval = new Vector2(1f, 2.4f);
        [SerializeField] private Vector2 _burstDuration = new Vector2(0.15f, 0.4f);
        [SerializeField] private Vector2 _burstMultiplierRange = new Vector2(1.5f, 2.6f);
        [Tooltip("Сжатие интервалов со временем — рост непредсказуемости")]
        [SerializeField] private float _unpredictabilityGrowth = 0.04f;

        [Header("Success Bar")]
        [SerializeField] private float _startSuccess = 50f;
        [SerializeField] private float _minSuccess = 0f;
        [SerializeField] private float _maxSuccess = 100f;
        [SerializeField] private float _regenPerSecond = 5f;
        [SerializeField] private float _drainBasePerSecond = 7f;
        [SerializeField] private float _drainRampPerSecond = 1.5f;
        [SerializeField] private float _drainRampCap = 80f;

        // --- Runtime ---
        private float _playerX;
        private float _playerVelocity;
        private float _holdTime;
        private float _idleTime;

        private float _inspectorX;
        private float _inspectorVelocity;
        private float _inspectorTargetX;
        private float _nextTargetTime;
        private float _nextBurstTime;
        private float _burstEndTime;
        private float _burstMultiplier = 1f;

        private float _success;
        private float _elapsed;
        private bool _finished;

        // =====================================================================
        //  Жизненный цикл
        // =====================================================================

        private void Awake()
        {
            if (_inspectorGameObject == null)
                Debug.LogError("[MinigameInspectorStop] inspectorGameObject is null");
            if (_playerGameObject == null)
                Debug.LogError("[MinigameInspectorStop] playerGameObject is null");
            
            // Берем размеры из RectTransform
            SetupCollider(_inspectorGameObject);
            SetupCollider(_playerGameObject);

            // Убираем физический отброс между игроком и инспектором
            var inspectorCol = _inspectorGameObject.GetComponent<Collider2D>();
            var playerCol = _playerGameObject.GetComponent<Collider2D>();
            if (inspectorCol != null && playerCol != null)
                Physics2D.IgnoreCollision(inspectorCol, playerCol, true);
        }

        private void SetupCollider(GameObject obj)
        {
            var rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogWarning($"[MinigameInspectorStop] {obj.name} не имеет RectTransform");
                return;
            }

            var col = obj.GetComponent<BoxCollider2D>();
            if (col == null) col = obj.AddComponent<BoxCollider2D>();

            // Берем размер из RectTransform с учетом масштаба
            Vector2 size = rectTransform.rect.size;
            Vector2 scale = rectTransform.lossyScale;
    
            // Учитываем масштаб родителя
            col.size = new Vector2(size.x * scale.x, size.y * scale.y);
    
            // Центрируем коллайдер (RectTransform обычно центрирован)
            col.offset = Vector2.zero;
        }

        private void FixedUpdate()
        {
            if (!_isPlaying || _finished) return;
            GameProcess();
        }

        private void Update()
        {
            if (!_isPlaying || _finished) return;

            if (_timeTMP != null)
                _timeTMP.SetText("Время: {0:F1}", _elapsed);
        }

        // =====================================================================
        //  База
        // =====================================================================

        protected override void GameInitializer(bool isPlaying)
        {
            _finished = false;
            _elapsed = 0f;
            _success = _startSuccess;
            _playerVelocity = 0f;
            _holdTime = 0f;
            _idleTime = 0f;

            _inspectorVelocity = 0f;
            _burstMultiplier = 1f;
            _burstEndTime = 0f;

            if (isPlaying)
            {
                _playerX = GetX(_playerGameObject.transform);
                _inspectorX = GetX(_inspectorGameObject.transform);
                _inspectorTargetX = _inspectorX;
                _nextTargetTime = 0.8f;
                _nextBurstTime = UnityEngine.Random.Range(0.8f, 2f);

                _inspectorGameObject.SetActive(true);
                _playerGameObject.SetActive(true);

                if (_timeTMP != null) _timeTMP.gameObject.SetActive(true);
                if (_successTMP != null) _successTMP.gameObject.SetActive(true);
                if (_successFill != null) _successFill.gameObject.SetActive(true);

                ApplyPositions();
                UpdateSuccessVisual();
            }
            else
            {
                _inspectorGameObject.SetActive(false);
                _playerGameObject.SetActive(false);

                if (_timeTMP != null) _timeTMP.gameObject.SetActive(false);
                if (_successTMP != null) _successTMP.gameObject.SetActive(false);
                if (_successFill != null) _successFill.gameObject.SetActive(false);
            }
        }

        protected override void GameProcess()
        {
            float dt = Time.fixedDeltaTime;
            _elapsed += dt;

            PlayerGameActions();
            InspectorGameActions();
            UpdateSuccessBar(dt);

            ApplyPositions();
        }

        // =====================================================================
        //  Игрок: удержание / покой
        // =====================================================================

        protected override void PlayerGameActions()
        {
            float dt = Time.fixedDeltaTime;

            bool holding = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;

            // Время удержания копится только пока держим, время покоя — только пока не держим.
            if (holding)
            {
                _idleTime = 0f;
                _holdTime = Mathf.Min(_holdTime + dt, _holdTimeCap);
            }
            else
            {
                _holdTime = 0f;
                _idleTime = Mathf.Min(_idleTime + dt, _idleTimeCap);
            }

            // Правое ускорение — линейно от времени удержания.
            float holdAccel = holding
                ? Mathf.Min(_holdAccelBase + _holdAccelPerSecond * _holdTime, _maxAccel)
                : 0f;

            // Левая «гравитация» — линейно от времени покоя (действует всегда).
            float gravityAccel = Mathf.Min(
                _gravityAccelBase + _gravityAccelPerSecond * _idleTime, _maxAccel);

            float accel = holdAccel - gravityAccel;

            _playerVelocity = Mathf.Clamp(_playerVelocity + accel * dt, -_maxSpeed, _maxSpeed);
            _playerX += _playerVelocity * dt;

            // Прижатие к краю обнуляет скорость в этом направлении.
            if (_playerX <= _leftBoundX)
            {
                _playerX = _leftBoundX;
                if (_playerVelocity < 0f) _playerVelocity = 0f;
            }
            else if (_playerX >= _rightBoundX)
            {
                _playerX = _rightBoundX;
                if (_playerVelocity > 0f) _playerVelocity = 0f;
            }
        }

        // =====================================================================
        //  Инспектор: случайные паттерны, рост скорости и непредсказуемости
        // =====================================================================

        private void InspectorGameActions()
        {
            float dt = Time.fixedDeltaTime;

            // Текущий потолок скорости (растёт экспоненциально).
            float speedCap = Mathf.Min(
                _inspectorBaseSpeed * Mathf.Pow(1f + _inspectorSpeedGrowthPerSecond, _elapsed),
                _inspectorMaxSpeed);

            // Коэффициент сжатия интервалов (< 1 со временем).
            float shrink = 1f / (1f + _unpredictabilityGrowth * _elapsed);

            if (_elapsed >= _nextTargetTime || Mathf.Abs(_inspectorTargetX - _inspectorX) < _inspectorArriveDistance)
                PickInspectorTarget(speedCap, shrink);

            if (_elapsed >= _burstEndTime)
                _burstMultiplier = 1f;

            if (_elapsed >= _nextBurstTime)
                StartInspectorBurst(shrink);

            // Движение к цели через ускорение — получаются «рваные» рывки.
            float dir = Mathf.Sign(_inspectorTargetX - _inspectorX);
            float targetVelocity = dir * speedCap * _burstMultiplier;

            _inspectorVelocity = Mathf.MoveTowards(
                _inspectorVelocity, targetVelocity, _inspectorAccel * dt);

            _inspectorX += _inspectorVelocity * dt;

            if (_inspectorX <= _leftBoundX)
            {
                _inspectorX = _leftBoundX;
                _inspectorVelocity = 0f;
                PickInspectorTarget(speedCap, shrink);
            }
            else if (_inspectorX >= _rightBoundX)
            {
                _inspectorX = _rightBoundX;
                _inspectorVelocity = 0f;
                PickInspectorTarget(speedCap, shrink);
            }
        }

        private void PickInspectorTarget(float speedCap, float shrink)
        {
            float width = _rightBoundX - _leftBoundX;
            float margin = Mathf.Min(width * 0.1f, width * 0.45f);

            _inspectorTargetX = UnityEngine.Random.Range(
                _leftBoundX + margin, _rightBoundX - margin);

            float interval = UnityEngine.Random.Range(_targetInterval.x, _targetInterval.y) * shrink;
            _nextTargetTime = _elapsed + interval;
        }

        private void StartInspectorBurst(float shrink)
        {
            _burstMultiplier = UnityEngine.Random.Range(_burstMultiplierRange.x, _burstMultiplierRange.y);
            _burstEndTime = _elapsed + UnityEngine.Random.Range(_burstDuration.x, _burstDuration.y) * shrink;
            _nextBurstTime = _elapsed + UnityEngine.Random.Range(_burstInterval.x, _burstInterval.y) * shrink;
        }

        // =====================================================================
        //  Шкала успешности
        // =====================================================================

        private void UpdateSuccessBar(float dt)
        {
            bool inspectorInside = Mathf.Abs(_inspectorX - _playerX) <= _playerHalfWidth;

            if (inspectorInside)
            {
                _success = Mathf.Min(_maxSuccess, _success + _regenPerSecond * dt);
            }
            else
            {
                // Убывание ускоряется со временем игры.
                float drain = _drainBasePerSecond +
                              Mathf.Min(_drainRampPerSecond * _elapsed, _drainRampCap);

                _success -= drain * dt;
            }

            _success = Mathf.Clamp(_success, _minSuccess, _maxSuccess);
            UpdateSuccessVisual();

            if (_success <= _minSuccess)
                FailGame();
        }

        private void UpdateSuccessVisual()
        {
            float t = Mathf.InverseLerp(_minSuccess, _maxSuccess, _success);

            if (_successFill != null)
            {
                _successFill.fillAmount = t;
                if (_successGradient != null)
                    _successFill.color = _successGradient.Evaluate(t);
            }

            if (_successTMP != null)
                _successTMP.SetText("{0:0}%", _success);
        }

        // =====================================================================
        //  Завершение
        // =====================================================================

        private void FailGame()
        {
            if (_finished) return;
            _finished = true;

            // Останавливаем собственные объекты.
            GameInitializer(false);

            // TODO: подставьте вызов завершения из вашего MinigameBase, например:
            // EndGame(false);
            // OnMinigameFinished?.Invoke(false);
            // _isPlaying = false;
            Debug.Log("[MinigameInspectorBalance] Поражение: шкала успешности на нуле.");
            LoseGame();
        }

        // =====================================================================
        //  Утилиты
        // =====================================================================

        private void ResolveBounds()
        {
            if (_leftEdge != null) _leftBoundX = GetX(_leftEdge);
            if (_rightEdge != null) _rightBoundX = GetX(_rightEdge);

            if (_rightBoundX < _leftBoundX)
            {
                float tmp = _leftBoundX;
                _leftBoundX = _rightBoundX;
                _rightBoundX = tmp;
            }
        }

        private void ApplyPositions()
        {
            if (_playerGameObject != null)
                SetX(_playerGameObject.transform, _playerX);

            if (_inspectorGameObject != null)
                SetX(_inspectorGameObject.transform, _inspectorX);
        }

        // Работаем в локальных координатах RectTransform, если объект — UI.
        private static float GetX(Transform t)
        {
            if (t is RectTransform rt) return rt.anchoredPosition.x;
            return t.position.x;
        }

        private static void SetX(Transform t, float x)
        {
            if (t is RectTransform rt)
            {
                Vector2 p = rt.anchoredPosition;
                p.x = x;
                rt.anchoredPosition = p;
            }
            else
            {
                Vector3 p = t.position;
                p.x = x;
                t.position = p;
            }
        }
    }
}