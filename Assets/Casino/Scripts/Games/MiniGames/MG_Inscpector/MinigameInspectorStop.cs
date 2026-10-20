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
                _inspectorGameObject.SetActive(true);
                _playerGameObject.SetActive(true);
                _holdingTimeTMP.gameObject.SetActive(true);
            }
            else
            {
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
            // Двигаем инспектора через MovePosition — физика учитывает столкновения
            Vector2 newPos = Vector2.MoveTowards(
                _inspectorRb.position,
                _exitPoint.transform.position,
                _currentInspectorSpeed * Time.fixedDeltaTime);

            _inspectorRb.MovePosition(newPos);
            _currentInspectorSpeed *= Mathf.Pow(_multiplierInspectorSpeed, Time.fixedDeltaTime);
        }
    }
}