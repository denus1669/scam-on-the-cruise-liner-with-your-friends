using System;
using System.Collections;
using Assets.Casino.Games.BlackGreg;
using TMPro;
using UnityEngine;

namespace Assets.Casino.Games.BlackGreg
{
    /// <summary>
    /// 3D-объект для отображения суммы руки.
    /// Размещается на сцене: один экземпляр для бота, один для игрока.
    /// </summary>
    public class HandScoreDisplay : MonoBehaviour
    {
        [SerializeField] private CardAnimationConfig animationConfig;
        [SerializeField] private TextMeshPro scoreText;

        private int _currentScore;
        private Vector3 _originalScale;
        private Coroutine _scoreUpdateCoroutine;

        private void Awake()
        {
            if (animationConfig == null)
                throw new InvalidOperationException("[HandScoreDisplay] CardAnimationConfig is not assigned");

            if (scoreText == null)
                throw new InvalidOperationException("[HandScoreDisplay] TextMeshPro is not assigned");

            _originalScale = transform.localScale;
            _currentScore = 0;
            scoreText.text = "0";

            // Скрываем счётчик при старте сцены/спавне
            SetVisible(false);
        }

        /// <summary>
        /// Обновляет счётчик с анимацией пульса.
        /// </summary>
        public void UpdateScore(int newValue)
        {
            if (_scoreUpdateCoroutine != null)
                StopCoroutine(_scoreUpdateCoroutine);

            _scoreUpdateCoroutine = StartCoroutine(ScoreUpdateRoutine(newValue));
        }

        private IEnumerator ScoreUpdateRoutine(int newValue)
        {
            Vector3 peakScale = _originalScale * animationConfig.scorePulseScale;
            float halfDuration = animationConfig.scorePulseDuration * 0.5f;
            float elapsed = 0f;

            // Увеличение масштаба
            while (elapsed < halfDuration)
            {
                transform.localScale = Vector3.Lerp(_originalScale, peakScale, elapsed / halfDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Обновляем число в момент пика пульса
            _currentScore = newValue;
            scoreText.text = _currentScore.ToString();

            // Уменьшение масштаба
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                transform.localScale = Vector3.Lerp(peakScale, _originalScale, elapsed / halfDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = _originalScale;
            _scoreUpdateCoroutine = null;
        }

        /// <summary>
        /// Сбрасывает счётчик на 0.
        /// </summary>
        public void ResetScore()
        {
            if (_scoreUpdateCoroutine != null)
            {
                StopCoroutine(_scoreUpdateCoroutine);
                _scoreUpdateCoroutine = null;
            }

            _currentScore = 0;
            scoreText.text = "0";
            transform.localScale = _originalScale;

            // Скрываем дисплей до следующего подсчёта
            SetVisible(false);
        }

        /// <summary>
        /// Точка, куда летят цифры от карт.
        /// </summary>
        public Vector3 GetTargetPosition() => transform.position;

        /// <summary>
        /// Показывает/скрывает отображение счёта.
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}