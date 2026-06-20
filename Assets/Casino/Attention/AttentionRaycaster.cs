
using Blocks.Gameplay.Core;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

    /// <summary>
    /// Выполняет рейкаст. Знает только об интерфейсах IAttentionTarget и IAccusable.
    /// Передает команды от игрока к объекту.
    /// </summary>
    public class AttentionRaycaster : NetworkBehaviour, IInteractable
    {
        [Header("Настройки луча")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask attentionLayerMask = ~0;
        [SerializeField] private float maxRayDistance = 10f;

        [Header("Ввод (Для Обвинения)")]
        [SerializeField] private CoreInputHandler inputHandler;

        private IAttentionTarget currentTarget;
        private Coroutine raycastCoroutine;

    public InteractionTriggerMode TriggerMode => throw new System.NotImplementedException();

    public int Priority => throw new System.NotImplementedException();

    public string InteractionPromptText => throw new System.NotImplementedException();

    public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            if (mainCamera == null) mainCamera = Camera.main;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;

            SetRaycasterActive(false);
        }

        /// <summary>
        /// Включает или выключает цикл пускания луча.
        /// </summary>
        public void SetRaycasterActive(bool isActive)
        {
            if (isActive)
            {
                if (raycastCoroutine == null)
                    raycastCoroutine = StartCoroutine(RaycastLoop());
            }
            else
            {
                if (raycastCoroutine != null)
                {
                    StopCoroutine(raycastCoroutine);
                    raycastCoroutine = null;
                }
                ClearCurrentTarget();
            }
        }

        private IEnumerator RaycastLoop()
        {
            while (true)
            {
                PerformRaycast();
                yield return null;
            }
        }

        private void PerformRaycast()
        {
            if (mainCamera == null) return;

            Vector3 origin = mainCamera.transform.position;
            Vector3 direction = mainCamera.transform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRayDistance, attentionLayerMask))
            {
                if (!hit.collider.isTrigger)
                {
                    ClearCurrentTarget();
                    return;
                }

                IAttentionTarget newTarget = hit.collider.GetComponentInParent<IAttentionTarget>();

                if (newTarget != currentTarget)
                {
                    ClearCurrentTarget();
                    currentTarget = newTarget;

                    if (currentTarget != null)
                    {
                        currentTarget.OnAttentionEnter(NetworkManager.Singleton.LocalClientId);
                    }
                }
            }
            else
            {
                ClearCurrentTarget();
            }
        }

        private void ClearCurrentTarget()
        {
            if (currentTarget != null)
            {
                currentTarget.OnAttentionExit(NetworkManager.Singleton.LocalClientId);
                currentTarget = null;
            }
        }
    

    /// <summary>
    /// Попытка обвинить цель, на которую сейчас смотрим (по нажатию 'E').
    /// </summary>
    private void TryAccuseTarget()
    {
        // Если объект поддерживает интерфейс обвинения, вызываем его
        if (currentTarget is IAccusable accusableTarget)
        {
            accusableTarget.OnAccuse(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void TryInteract()
    {
        // Мы всё еще используем IAttentionTarget для визуальной подсветки, 
        // но для действия E используем IInteractable
        if (currentTarget is IInteractable interactable)
        {
            if (interactable.CanInteract(gameObject))
            {
                interactable.Interact(gameObject);
            }
        }
    }

    public bool CanInteract(GameObject interactor)
    {
        // Можно добавить логику, например, проверяем, находится ли бот в состоянии мухлежа
        return true;
    }

    public void Interact(GameObject interactor)
    {
        if (interactor.TryGetComponent<NetworkObject>(out var networkObject))
        {
            AccuseServerRpc(networkObject.OwnerClientId);
        }
    }

    [Rpc(SendTo.Server)]
    private void AccuseServerRpc(ulong accuserClientId)
    {
        Debug.Log($"[Обвинение] Игрок {accuserClientId} обвинил бота {gameObject.name} в мухлеже!");
        // Здесь логика проверки мухлежа
    }
}
