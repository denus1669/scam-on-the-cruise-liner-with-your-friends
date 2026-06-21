
using Blocks.Gameplay.Core;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

    /// <summary>
    /// Выполняет рейкаст. Знает только об интерфейсах IAttentionTarget.
    /// Передает команды от игрока к объекту.
    /// </summary>
    public class AttentionRaycaster : NetworkBehaviour
    {
        [Header("Настройки луча")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask attentionLayerMask = ~0;
        [SerializeField] private float maxRayDistance = 10f;

        private IAttentionTarget currentTarget;
        private Coroutine raycastCoroutine;

        // Событие: Луч попал на валидную цель (передаем саму цель, если подписчикам нужны ее данные)
        public event Action<IAttentionTarget> OnTargetEnterLocal;

        // Событие: Луч сошел с цели или сканер был выключен
        public event Action OnTargetExitLocal;

        // Публичное свойство для доступа к текущей цели в любой момент, если кому-то нужно проверить состояние без подписки
        public IAttentionTarget CurrentTarget => currentTarget;
        public bool HasTarget => currentTarget != null;

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
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void PerformRaycast()
        {
            if (mainCamera == null) return;

            Vector3 origin = mainCamera.transform.position;
            Vector3 direction = mainCamera.transform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRayDistance, attentionLayerMask))
            {
                Debug.DrawLine(origin, hit.point, Color.green, 0.1f);

                if (!hit.collider.isTrigger)
                {
                    ClearCurrentTarget();
                    return;
                }

                IAttentionTarget newTarget = hit.collider.GetComponent<IAttentionTarget>();     

                if (newTarget != currentTarget)
                {
                    ClearCurrentTarget();
                    currentTarget = newTarget;

                    if (currentTarget != null)
                    {
                        // 1. Вызываем метод самого интерфейса (возможно, там RPC логика самой цели)
                        currentTarget.OnAttentionEnter(NetworkManager.Singleton.LocalClientId);

                        // 2. Оповещаем все наши локальные скрипты (UI, Анализаторы, Звуки), что мы смотрим на цель
                        OnTargetEnterLocal?.Invoke(currentTarget);
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
}
