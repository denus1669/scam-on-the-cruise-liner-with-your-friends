using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Отвечает ТОЛЬКО за передачу информации от приемника внимания 
    /// в контроллер недовольства бота.
    /// </summary>
    [RequireComponent(typeof(AttentionTargetReceiver))]
    public class BotDispleasureAttentionHandler : MonoBehaviour
    {
        [Tooltip("Ссылка на контроллер недовольства бота")]
        [SerializeField] private BotDispleasureController botDispleasure;

        private AttentionTargetReceiver _attentionReceiver;

        private void Awake()
        {
            _attentionReceiver = GetComponent<AttentionTargetReceiver>();
        }

        private void Reset()
        {
            // Автопоиск при добавлении скрипта в редакторе
            botDispleasure = GetComponentInParent<BotDispleasureController>();
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

        private void HandleAttentionEnter(ulong watcherClientId)
        {
            if (botDispleasure != null)
            {
                botDispleasure.AddWatcherServerRpc(watcherClientId);
            }
        }

        private void HandleAttentionExit(ulong watcherClientId)
        {
            if (botDispleasure != null)
            {
                botDispleasure.RemoveWatcherServerRpc(watcherClientId);
            }
        }
    }
}