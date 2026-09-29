using System.Collections;
using System.Collections.Generic;
using Assets.Casino.Attention;
using Assets.Casino.Cheating;
using Assets.Casino.Games.BlackGreg;
using UnityEngine;

namespace Assets.Casino.Scripts.Actions.Attention
{
    /// <summary>
    /// Подсвечивает карты бота, когда игрок смотрит на него в режиме внимания
    /// и бот находится в процессе мухлежа.
    /// </summary>
    [RequireComponent(typeof(AttentionTargetReceiver))]
    public class BotCheatAttentionHighlighter : MonoBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private CheatCardHighlightConfig highlightConfig;
        [Tooltip("Корень, в котором находятся карты бота. Если не указан, поиск в детях объекта.")]
        [SerializeField] private Transform cardsRoot;

        [SerializeField] private CheatController _cheatController;
        private AttentionTargetReceiver _attentionReceiver;
        private Coroutine _fadeCoroutine;

        private bool _isWatched;
        private float _currentIntensity;

        private readonly List<CardView> _cardBuffer = new List<CardView>();

        private void Awake()
        {
            if(_cheatController == null)
                _cheatController = GetComponent<CheatController>();
            if (_attentionReceiver == null)
                _attentionReceiver = GetComponent<AttentionTargetReceiver>();
            if (cardsRoot == null)
                cardsRoot = transform;
        }

        private void OnEnable()
        {
            _cheatController.OnCheatingStateChanged += HandleCheatingStateChanged;
            _attentionReceiver.OnAttentionEntered += HandleAttentionEntered;
            _attentionReceiver.OnAttentionExited += HandleAttentionExited;
        }

        private void OnDisable()
        {
            _cheatController.OnCheatingStateChanged -= HandleCheatingStateChanged;
            _attentionReceiver.OnAttentionEntered -= HandleAttentionEntered;
            _attentionReceiver.OnAttentionExited -= HandleAttentionExited;
        }

        private void HandleCheatingStateChanged(bool isCheating)
        {
            UpdateHighlightState();
        }

        private void HandleAttentionEntered(ulong watcherClientId)
        {
            _isWatched = true;
            UpdateHighlightState();
        }

        private void HandleAttentionExited(ulong watcherClientId)
        {
            _isWatched = false;
            UpdateHighlightState();
        }

        private void UpdateHighlightState()
        {
            bool shouldHighlight = _isWatched && _cheatController.IsCheating;
            float targetIntensity = shouldHighlight ? highlightConfig.highlightIntensity : 0f;
            float duration = shouldHighlight ? highlightConfig.fadeInDuration : highlightConfig.fadeOutDuration;

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeToIntensity(targetIntensity, duration));
        }

        private IEnumerator FadeToIntensity(float target, float duration)
        {
            float start = _currentIntensity;

            if (duration <= 0f)
            {
                _currentIntensity = target;
                ApplyIntensityToCards();
                _fadeCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _currentIntensity = Mathf.Lerp(start, target, elapsed / duration);
                ApplyIntensityToCards();
                yield return null;
            }

            _currentIntensity = target;
            ApplyIntensityToCards();
            _fadeCoroutine = null;
        }

        private void ApplyIntensityToCards()
        {
            _cardBuffer.Clear();
            cardsRoot.GetComponentsInChildren(true, _cardBuffer);

            foreach (var card in _cardBuffer)
            {
                card.SetCheatHighlight(_currentIntensity);
            }
        }
    }
}