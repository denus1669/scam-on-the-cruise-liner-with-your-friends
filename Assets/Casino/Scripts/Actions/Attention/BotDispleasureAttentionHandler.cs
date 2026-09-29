using Assets.Casino.Bot;
using UnityEngine;
using Unity.Netcode;

namespace Assets.Casino.Attention
{
    [RequireComponent(typeof(AttentionTargetReceiver))]
    public class BotDispleasureAttentionHandler : MonoBehaviour
    {
        [Tooltip("Ссылка на контроллер недовольства бота")]
        [SerializeField] private BotDispleasureController botDispleasure;

        private AttentionTargetReceiver _attentionReceiver;
        private ulong _localClientId;

        private void Awake()
        {
            _attentionReceiver = GetComponent<AttentionTargetReceiver>();

            // Кэшируем ID локального клиента при старте, чтобы использовать его в OnDisable
            if (NetworkManager.Singleton != null)
            {
                _localClientId = NetworkManager.Singleton.LocalClientId;
            }
        }

        private void Reset()
        {
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

            // ГАРАНТИЯ: При уничтожении или деактивации этого компонента 
            // мы принудительно сообщаем серверу, что игрок перестал смотреть.
            // Это предотвращает "вечное" раздражение бота при дисконнекте игрока.
            if (botDispleasure != null && _localClientId != 0)
            {
                botDispleasure.RemoveWatcherServerRpc(_localClientId);
            }
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