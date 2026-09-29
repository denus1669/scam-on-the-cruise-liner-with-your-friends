using System.Collections;
using Assets.Casino.Games.BlackGreg;
using UnityEngine;

namespace Assets.Casino.Attention
{
    [RequireComponent(typeof(AttentionTargetReceiver))]
    public class CardHandInspector : MonoBehaviour
    {
        private AttentionTargetReceiver _attentionReceiver;
        [SerializeField] private CardView[] _currentCards;

        private bool _isFocused;
        private Coroutine _refreshCoroutine;

        private void Awake()
        {
            _attentionReceiver = GetComponent<AttentionTargetReceiver>();
        }

        private void OnEnable()
        {
            _attentionReceiver.OnAttentionEntered += HandleAttentionEnter;
            _attentionReceiver.OnAttentionExited += HandleAttentionExit;
        }

        private void OnDisable()
        {
            _attentionReceiver.OnAttentionEntered -= HandleAttentionEnter;
            _attentionReceiver.OnAttentionExited -= HandleAttentionExit;
        }

        private void OnTransformChildrenChanged()
        {
            if (!_isFocused) return;

            // Останавливаем предыдущую корутину, если карты добавляются быстро
            if (_refreshCoroutine != null)
                StopCoroutine(_refreshCoroutine);

            _refreshCoroutine = StartCoroutine(RefreshNextFrame());
        }

        /// <summary>
        /// Ждёт конец кадра, чтобы все операции с картой 
        /// (Instantiate, SetCardData, SetVisible) завершились.
        /// </summary>
        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            RefreshCards(true);
            _refreshCoroutine = null;
        }

        private void HandleAttentionEnter(ulong watcherClientId)
        {
            _isFocused = true;
            RefreshCards(true);
        }

        private void HandleAttentionExit(ulong watcherClientId)
        {
            _isFocused = false;
            RefreshCards(false);
        }

        private void RefreshCards(bool isVisible)
        {
            _currentCards = GetComponentsInChildren<CardView>(true);

            if (_currentCards != null)
            {
                foreach (var card in _currentCards)
                {
                    if (card != null)
                        card.SetVisible(isVisible);
                }
            }
        }
    }
}