using Assets.Casino.Test_Folder;
using System;
using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Assets.Casino.Games.BlackGreg
{
    [RequireComponent(typeof(CardVisualController))]
    public class CardView : MonoBehaviour
    {
        [SerializeField] private CardAnimationConfig animationConfig;
        [SerializeField] private Transform flyingNumberObject;
        [SerializeField] private TextMeshPro flyingNumberText;
        [SerializeField] private Highlither highlither;
        private CardVisualController _visualController;
        private CardData _currentData;
        private Vector3 _originalScale;

        private void Awake()
        {
            if (animationConfig == null)
                throw new InvalidOperationException($"[CardView] CardAnimationConfig is not assigned on {gameObject.name}");

            if (flyingNumberObject == null || flyingNumberText == null)
                throw new InvalidOperationException($"[CardView] FlyingNumber references are not assigned on {gameObject.name}");

            _visualController = GetComponent<CardVisualController>();
            _originalScale = transform.localScale;
            flyingNumberObject.gameObject.SetActive(false);
        }

        public void SetCardData(CardData data)
        {
            _currentData = data;
            Vector2 newFaceIndex = CardVisualMapper.GetFaceIndex(data);

            if (_visualController == null) _visualController = GetComponent<CardVisualController>();
            _visualController.faceIndex = newFaceIndex;
            _visualController.UpdateCardVisuals();
        }

        public void SetVisible(bool isVisible)
        {
            if (_visualController == null) _visualController = GetComponent<CardVisualController>();
            _visualController.isVisible = isVisible;
            _visualController.UpdateCardVisuals();
        }

        public CardData GetCardData() => _currentData;

        // ========== Подсветка ==========

        /// <summary>
        /// Включает подсветку карты указанным цветом.
        /// </summary>

        public void SetHighlight(bool isEnable, Color color)
        {
            highlither.SetOutlineEnable(isEnable);
            highlither.SetOutlineColor(color);
        }

        // ========== Анимация подсчёта (пульсация) ==========

        /// <summary>
        /// Запускает пульсацию карты при подсчёте очков.
        /// </summary>
        public void PlayCountingPulse()
        {
            StartCoroutine(CountingPulseRoutine());
        }

        private IEnumerator CountingPulseRoutine()
        {
            Vector3 startScale = _originalScale;
            Vector3 peakScale = _originalScale * animationConfig.countingPulseScale;
            float halfDuration = animationConfig.countingPulseDuration * 0.5f;
            float elapsed = 0f;

            // Увеличение
            while (elapsed < halfDuration)
            {
                transform.localScale = Vector3.Lerp(startScale, peakScale, elapsed / halfDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Уменьшение
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                transform.localScale = Vector3.Lerp(peakScale, startScale, elapsed / halfDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = startScale;
        }

        // ========== Анимация победителя (подъём без возврата) ==========

        /// <summary>
        /// Поднимает карту вверх без возврата на место.
        /// </summary>
        public void PlayJumpAnimation()
        {
            StartCoroutine(JumpRoutine());
        }

        private IEnumerator JumpRoutine()
        {
            Vector3 startPos = transform.localPosition;
            Vector3 targetPos = startPos + Vector3.up * animationConfig.winnerJumpHeight;
            float elapsed = 0f;

            while (elapsed < animationConfig.winnerJumpDuration)
            {
                transform.localPosition = Vector3.Lerp(startPos, targetPos, elapsed / animationConfig.winnerJumpDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = targetPos;
        }

        // ========== Летящая цифра ==========

        /// <summary>
        /// Показывает цифру над картой и отправляет её к целевой точке.
        /// </summary>
        public void ShowFlyingNumber(int value, Vector3 target, Action onComplete)
        {
            StartCoroutine(FlyingNumberRoutine(value, target, onComplete));
        }

        private IEnumerator FlyingNumberRoutine(int value, Vector3 target, Action onComplete)
        {
            flyingNumberText.text = value.ToString();
            flyingNumberObject.gameObject.SetActive(true);

            Vector3 startPos = flyingNumberObject.position;
            float distance = Vector3.Distance(startPos, target);
            float duration = distance / animationConfig.flyingNumberSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                flyingNumberObject.position = Vector3.Lerp(startPos, target, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            flyingNumberObject.position = target;
            flyingNumberObject.gameObject.SetActive(false);

            onComplete?.Invoke();
        }

        // ========== Сброс карты в колоду ==========

        /// <summary>
        /// Запускает анимацию сброса карты с каскадной задержкой.
        /// </summary>
        public void FlyToDiscard(Transform discardPile, float delay = 0f)
        {
            if (discardPile == null)
            {
                Destroy(gameObject);
                return;
            }
            StartCoroutine(FlyToDiscardRoutine(discardPile, delay));
        }

        private IEnumerator FlyToDiscardRoutine(Transform discardPile, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            highlither.SetOutlineEnable(false); // Отключаем подсветку перед полётом


            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Vector3 startScale = transform.localScale;

            Vector3 targetPos = discardPile.position;
            Quaternion targetRot = discardPile.rotation;
            float elapsed = 0f;

            while (elapsed < animationConfig.flyDuration)
            {
                float t = animationConfig.flyCurve.Evaluate(elapsed / animationConfig.flyDuration);

                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                transform.Rotate(Vector3.up, animationConfig.flyRotationSpeed * Time.deltaTime);
                transform.localScale = Vector3.Lerp(startScale, animationConfig.targetScale, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
