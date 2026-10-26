using Assets.Casino.Cheating.MiniGames;
using UnityEngine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;


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
    public class MGInspectorBalance : MinigameBase
    {
        [Header("MG GameObjects")] [SerializeField]
        private GameObject inspectorGameObject;

        [SerializeField] private GameObject playerGameObject;

        [Header("UI")] [SerializeField] private TextMeshProUGUI timeTMP;
        [SerializeField] private TextMeshProUGUI successTMP;
        [SerializeField] private Image successFill; // Image с Type = Filled, Horizontal
        [SerializeField] private Gradient successGradient; // красный -> зелёный (опционально)

        [Header("Play Area (горизонтальная зона)")]
        [Tooltip("Крайние точки зоны. Если не заданы — используются числа ниже.")]
        [SerializeField] private Transform leftEdge;
        [SerializeField] private Transform rightEdge;

        #region Variables
        
        [Header("Player Balance")]
        [SerializeField] private float holdAccelBase = 300f;   // Базовое ускорение вправо в первый миг удержания (px/s²)
        [SerializeField] private float holdAccelPerSecond = 1400f;   // Прирост ускорения вправо за каждую секунду удержания (px/s² за сек)
        [SerializeField] private float gravityAccelBase = 200f; // Базовое ускорение «гравитации» влево в первый миг покоя (px/s²)
        [SerializeField] private float gravityAccelPerSecond = 1400f; // Прирост «гравитации» влево за каждую секунду покоя (px/s² за сек)
        [SerializeField] private float maxAccel = 4000f; // Потолок ускорения (чтобы не улетало в бесконечность)
        [SerializeField] private float maxSpeed = 1800f; // Потолок скорости силуэта игрока по модулю (px/s)
        [SerializeField] private float holdTimeCap = 2f; // Максимум, до которого копится _holdTime (сек)
        [SerializeField] private float idleTimeCap = 2f; // Максимум, до которого копится _idleTime (сек)
        
        [Header("Inspector AI")]
        [SerializeField] private float inspectorBaseSpeed = 150f;
        [SerializeField] private float inspectorMaxSpeed = 900f;
        [Tooltip("Экспоненциальный рост скорости: speed = base * (1 + g)^t")]
        [SerializeField] private float inspectorSpeedGrowthPerSecond = 0.05f;
        [SerializeField] private float inspectorAccel = 1200f;
        [SerializeField] private float inspectorArriveDistance = 10f;
        [SerializeField] private Vector2 targetInterval = new Vector2(0.6f, 1.4f);
        [SerializeField] private Vector2 burstInterval = new Vector2(1f, 2.4f);
        [SerializeField] private Vector2 burstDuration = new Vector2(0.15f, 0.4f);
        [SerializeField] private Vector2 burstMultiplierRange = new Vector2(1.5f, 2.6f);
        [Tooltip("Сжатие интервалов со временем — рост непредсказуемости")]
        [SerializeField] private float unpredictabilityGrowth = 0.04f;
        
        [Header("Success Bar")]
        [SerializeField] private float startSuccess = 50f;
        [SerializeField] private float minSuccess = 0f;
        [SerializeField] private float maxSuccess = 100f;
        [SerializeField] private float regenPerSecond = 5f;
        [SerializeField] private float drainBasePerSecond = 7f;
        [SerializeField] private float drainRampPerSecond = 1.5f;
        [SerializeField] private float drainRampCap = 80f;
        
        // --- Runtime ---
        private RectTransform _leftEdgeRT;
        private RectTransform _rightEdgeRT;
        
        private float _leftBoundX;
        private float _rightBoundX;
        
        private float _playerHalfWidth;
        private float _playerX;              // Текущая X-позиция силуэта игрока (в координатах родителя/anchoredPosition)
        private float _playerVelocity;       // Текущая X-скорость силуэта игрока (px/s), обнуляется при упоре в край
        private float _holdTime;             // Сколько секунд подряд игрок ДЕРЖИТ Пробел (обнуляется при отпускании)
        private float _idleTime;             // Сколько секунд подряд игрок НЕ держит Пробел (обнуляется при нажатии)

        private float _inspectorX;           // Текущая X-позиция инспектора
        private float _inspectorVelocity;    // Текущая X-скорость инспектора (px/s), обнуляется при упоре в край
        private float _inspectorTargetX;     // X-координата точки, к которой сейчас летит инспектор
        private float _nextTargetTime;       // Момент времени (_elapsed), когда принудительно выбрать новую цель инспектора
        private float _nextBurstTime;        // Момент времени (_elapsed), когда начнётся следующий «рывок» инспектора
        private float _burstEndTime;         // Момент времени (_elapsed), когда текущий «рывок» закончится
        private float _burstMultiplier = 1f; // Множитель скорости на время рывка (1 = без ускорения)

        private float _success;              // Текущее значение шкалы успешности (%), 0 = поражение, 100 = максимум
        private float _elapsed;              // Время игры в секундах с момента старта (глобальные часы прогрессии)
        private bool _finished;              // Флаг завершения игры (защита от повторного срабатывания FailGame)
        

        #endregion
        
        #region BaseGame

        private void Awake()
        {
            if (inspectorGameObject == null)
                Debug.LogError("[MinigameInspectorStop] inspectorGameObject is null");
            if (playerGameObject == null)
                Debug.LogError("[MinigameInspectorStop] playerGameObject is null");
            
            RectTransform playerRT = playerGameObject.GetComponent<RectTransform>();
            RectTransform inspectorRT = inspectorGameObject.GetComponent<RectTransform>(); 
            _leftEdgeRT = leftEdge.transform.GetComponent<RectTransform>();
            _rightEdgeRT = rightEdge.transform.GetComponent<RectTransform>();

            _playerHalfWidth = playerRT.rect.width  * playerRT.lossyScale.x;
        }
        
        private void FixedUpdate()
        {
            if (!_isPlaying || _finished) return;
            GameProcess();
        }

        private void Update()
        {
            if (!_isPlaying || _finished) return;

            if (timeTMP != null)
                timeTMP.SetText("Время: {0:F1}", _elapsed);
        }

        protected override void GameInitializer(bool isPlaying)
        {
            _finished = false;
            _elapsed = 0f;
            _success = startSuccess;
            
            _playerVelocity = 0f;
            _holdTime = 0f;
            _idleTime = 0f;

            _inspectorVelocity = 0f;
            _burstMultiplier = 1f;
            _burstEndTime = 0f;

            if (isPlaying)
            {
                _playerX = GetX(playerGameObject.transform);
                _inspectorX = GetX(inspectorGameObject.transform);
                _inspectorTargetX = _inspectorX;
                _nextTargetTime = 0.8f;
                _nextBurstTime = UnityEngine.Random.Range(0.8f, 2f);

                _leftBoundX = _leftEdgeRT.localPosition.x + _playerHalfWidth;
                _rightBoundX = _rightEdgeRT.localPosition.x - _playerHalfWidth;
                
                ApplyPositions();
                UpdateSuccessVisual();
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
        //  Завершение
        // =====================================================================

        private void FailGame()
        {
            // Останавливаем собственные объекты.
            GameInitializer(false);
            
            Debug.Log("[MinigameInspectorBalance] Поражение: шкала успешности на нуле.");
            LoseGame();
        }
        
        #endregion

        #region Player Actions

        protected override void PlayerGameActions()
        {
            float dt = Time.fixedDeltaTime;

            bool holding = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;

            // Время удержания копится только пока держим, время покоя — только пока не держим.
            if (holding)
            {
                _idleTime = 0f;
                _holdTime = Mathf.Min(_holdTime + dt, holdTimeCap);
            }
            else
            {
                _holdTime = 0f;
                _idleTime = Mathf.Min(_idleTime + dt, idleTimeCap);
            }

            // Правое ускорение — линейно от времени удержания.
            float holdAccel = holding
                ? Mathf.Min(holdAccelBase + holdAccelPerSecond * _holdTime, maxAccel)
                : 0f;

            // Левая «гравитация» — линейно от времени покоя (действует всегда).
            float gravityAccel = Mathf.Min(
                gravityAccelBase + gravityAccelPerSecond * _idleTime, maxAccel);

            float accel = holdAccel - gravityAccel;

            _playerVelocity = Mathf.Clamp(_playerVelocity + accel * dt, -maxSpeed, maxSpeed);
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

        #endregion

        #region Inspector Logic

        private void InspectorGameActions()
        {
            float dt = Time.fixedDeltaTime;

            // Текущий потолок скорости (растёт экспоненциально).
            float speedCap = Mathf.Min(
                inspectorBaseSpeed * Mathf.Pow(1f + inspectorSpeedGrowthPerSecond, _elapsed),
                inspectorMaxSpeed);

            // Коэффициент сжатия интервалов (< 1 со временем).
            float shrink = 1f / (1f + unpredictabilityGrowth * _elapsed);

            if (_elapsed >= _nextTargetTime || Mathf.Abs(_inspectorTargetX - _inspectorX) < inspectorArriveDistance)
                PickInspectorTarget(speedCap, shrink);

            if (_elapsed >= _burstEndTime)
                _burstMultiplier = 1f;

            if (_elapsed >= _nextBurstTime)
                StartInspectorBurst(shrink);

            // Движение к цели через ускорение — получаются «рваные» рывки.
            float dir = Mathf.Sign(_inspectorTargetX - _inspectorX);
            float targetVelocity = dir * speedCap * _burstMultiplier;

            _inspectorVelocity = Mathf.MoveTowards(
                _inspectorVelocity, targetVelocity, inspectorAccel * dt);

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

            float interval = UnityEngine.Random.Range(targetInterval.x, targetInterval.y) * shrink;
            _nextTargetTime = _elapsed + interval;
        }

        private void StartInspectorBurst(float shrink)
        {
            _burstMultiplier = UnityEngine.Random.Range(burstMultiplierRange.x, burstMultiplierRange.y);
            _burstEndTime = _elapsed + UnityEngine.Random.Range(burstDuration.x, burstDuration.y) * shrink;
            _nextBurstTime = _elapsed + UnityEngine.Random.Range(burstInterval.x, burstInterval.y) * shrink;
        }

        #endregion

        #region Utility
        
        private void ApplyPositions()
        {
            if (playerGameObject != null)
                SetX(playerGameObject.transform, _playerX);

            if (inspectorGameObject != null)
                SetX(inspectorGameObject.transform, _inspectorX);
        }
        // Работаем в локальных координатах RectTransform, если объект — UI.
        private static float GetX(Transform t)
        {
            if (t is RectTransform rt) return rt.anchoredPosition.x;
            return t.position.x;
        }

        private static void SetX(Transform transform, float newX)
        {
            if (transform is RectTransform rectTransform)
            {
                Vector2 position = rectTransform.anchoredPosition;
                position.x = newX;
                rectTransform.anchoredPosition = position;
            }
            else
            {
                Vector3 position = transform.position;
                position.x = newX;
                transform.position = position;
            }
        }

        #endregion

        #region UI

        // =====================================================================
        //  Шкала успешности
        // =====================================================================

        private void UpdateSuccessBar(float dt)
        {
            bool inspectorInside = Mathf.Abs(_inspectorX - _playerX) <= _playerHalfWidth;

            if (inspectorInside)
            {
                _success = Mathf.Min(maxSuccess, _success + regenPerSecond * dt);
            }
            else
            {
                // Убывание ускоряется со временем игры.
                float drain = drainBasePerSecond +
                              Mathf.Min(drainRampPerSecond * _elapsed, drainRampCap);

                _success -= drain * dt;
            }

            _success = Mathf.Clamp(_success, minSuccess, maxSuccess);
            UpdateSuccessVisual();

            if (_success <= minSuccess)
                FailGame();
        }

        private void UpdateSuccessVisual()
        {
            float t = Mathf.InverseLerp(minSuccess, maxSuccess, _success);

            if (successFill != null)
            {
                successFill.fillAmount = t;
                if (successGradient != null)
                    successFill.color = successGradient.Evaluate(t);
            }

            if (successTMP != null)
                successTMP.SetText("{0:0}%", _success);
        }

        #endregion
        

    }
}