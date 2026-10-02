using UnityEngine;

namespace Assets.Casino.Games.BlackGreg
{
    [CreateAssetMenu(fileName = "CardAnimationConfig", menuName = "Casino/Config/Card Animation")]
    public class CardAnimationConfig : ScriptableObject
    {
        [Header("Counting Animation")]
        public Color countingHighlightColor = Color.cyan;
        public float countingDelayBetweenCards = 0.2f;
        public float countingPulseDuration = 0.3f;
        public float countingPulseScale = 1.2f;

        [Header("Draw Card Animation")]
        public float drawCardDuration = 0.5f;
        public AnimationCurve drawCardCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Draw Animation")]
        public Color drawHighlightColor = Color.white;
        public float drawLiftHeight = 0.1f;
        public float drawLiftDuration = 0.4f;

        [Header("Flying Number")]
        public float flyingNumberSpeed = 5f;

        [Header("Winner Animation")]
        public Color winnerHighlightColor = new Color(1f, 0.85f, 0f); // Gold
        public float winnerJumpHeight = 0.2f;
        public float winnerJumpDuration = 0.3f;

        [Header("Score Display")]
        public float scorePulseDuration = 0.3f;
        public float scorePulseScale = 1.5f;

        [Header("Discard (Fly to Deck)")]
        public float flyDuration = 0.6f;
        public Vector3 targetScale = Vector3.zero;
        public AnimationCurve flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float flyRotationSpeed = 360f;
        public float discardStaggerDelay = 0.1f;
    }
}