using System;
using Unity.Netcode;
using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Вешается на префаб игрока. Принимает вызов шлепка и рассылает 
    /// глобальные события для проигрывания анимаций и звуков получения урона.
    /// </summary>
    public class PlayerSlapReceiver : NetworkBehaviour, IInteractable
    {
        public InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;
        public int Priority => 10; // Выше обычных предметов
        public string InteractionPromptText => "[E] Сломать кабину игроку";

        public float HoldDuration => 0f;

        // Событие для визуальных скриптов: "Меня ударил вот этот игрок"
        public event Action<ulong> OnGlobalReceivedSlap;

        public void ExecuteSlap(ulong interactorClientId)
        {
            if (!IsServer)
            {
                ReceiveSlapServerRpc(interactorClientId);
            }
            else
            {
                ProcessSlap(interactorClientId);
            }
        }

        [Rpc(SendTo.Server)]
        private void ReceiveSlapServerRpc(ulong interactorClientId)
        {
            ProcessSlap(interactorClientId);
        }

        private void ProcessSlap(ulong interactorClientId)
        {
            Debug.Log($"[Server] Игрок {OwnerClientId} получил шлепок от {interactorClientId}.");

            

            // TODO: Здесь будет логика штрафов (если нужно)

            // Оповещаем всех клиентов, чтобы они проиграли анимацию получения шлепка
            NotifySlapClientRpc(interactorClientId);
        }

        [Rpc(SendTo.Everyone)]
        private void NotifySlapClientRpc(ulong interactorClientId)
        {
            Debug.Log($"[Client] Игрок {OwnerClientId} получил по рукам! (Здесь будет VFX/Анимация)");
            OnGlobalReceivedSlap?.Invoke(interactorClientId);
        }

        public bool CanInteract(GameObject interactor)
        {
            // Нельзя шлепать себя
            if (interactor.TryGetComponent<NetworkObject>(out var netObj))
                return netObj.OwnerClientId != OwnerClientId;
            return true;
        }

        public void Interact(GameObject interactor)
        {
            ulong slapperId = interactor.GetComponent<NetworkObject>().OwnerClientId;
            ExecuteSlap(slapperId);
        }
    }
}