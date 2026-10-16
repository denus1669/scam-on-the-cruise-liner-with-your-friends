using Assets.Casino.Cheating.MiniGames;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Casino.Scripts.Actions.Cheating.MiniGames
{
    /// <summary>
    /// Мини-игра: нажать кнопку, когда ползунок в зеленой зоне.
    /// </summary>
    public class Minigame_ReactionClick : MinigameBase
    {
        [Header("Настройки Реакции")]
        [SerializeField] private Slider progressSlider;
        [SerializeField] private float speed = 2f;
        [SerializeField] private float targetMin = 0.2f;
        [SerializeField] private float targetMax = 0.8f;

        [Header("Visuals")]
        [SerializeField] private RectTransform successZoneVisual; // Опционально: маркер зоны

        private bool _movingRight = true;

        protected override void OnGameStarted()
        {
            base.OnGameStarted();
            _movingRight = true;

            if (progressSlider)
                progressSlider.value = 0f;

            // Можно использовать _contextData для изменения сложности, если нужно
            // Например: if (_contextData == "HardCheat") speed *= 1.5f;
        }

        private void Update()
        {
            if (!_isPlaying) return;

            if (progressSlider)
            {
                float step = speed * Time.deltaTime;
                progressSlider.value += _movingRight ? step : -step;

                if (progressSlider.value >= 1f) _movingRight = false;
                if (progressSlider.value <= 0f) _movingRight = true;
            }

            // Ввод: Пробел 
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                CheckResult();
            }
        }

        private void CheckResult()
        {
            float val = progressSlider != null ? progressSlider.value : 0f;

            if (val >= targetMin && val <= targetMax)
            {
                Debug.Log("[Minigame] Победа! (Реакция)");
                WinGame();
            }
            else
            {
                Debug.Log("[Minigame] Промах! (Реакция)");
            }
        }
    }
}