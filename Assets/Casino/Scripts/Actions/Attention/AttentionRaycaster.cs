using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Attention
{
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
        [SerializeField] private float raycastInterval = 0.1f; // <-- Добавлено: частота проверки (10 раз в секунду)


        private IAttentionTarget currentTarget;
        private Coroutine raycastCoroutine;

        // Событие: Луч попал на валидную цель (передаем саму цель, если подписчикам нужны ее данные)
        public event Action<IAttentionTarget> OnTargetEnterLocal;

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
                yield return new WaitForSeconds(raycastInterval);
            }
        }

        private void PerformRaycast()
        {
            if (mainCamera == null) return;

            Vector3 origin = mainCamera.transform.position;
            Vector3 direction = mainCamera.transform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRayDistance, attentionLayerMask))
            {
                // Если попали в триггер
                if (hit.collider.isTrigger)
                {
                    IAttentionTarget newTarget = hit.collider.GetComponent<IAttentionTarget>();

                    // Обновляем цель ТОЛЬКО если она реально изменилась
                    if (newTarget != currentTarget)
                    {
                        ClearCurrentTarget();
                        currentTarget = newTarget;

                        if (currentTarget != null)
                        {
                            currentTarget.OnAttentionEnter(NetworkManager.Singleton.LocalClientId);
                            // OnTargetEnterLocal?.Invoke(currentTarget); // Оставляем, если нужно UI
                        }
                    }
                }
                else
                {
                    // Попали не в триггер - сбрасываем цель
                    ClearCurrentTarget();
                }
            }
            else
            {
                // Ни во что не попали - сбрасываем цель
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

        private void OnDisable()
        {
            // Гарантируем очистку цели при деактивации компонента, 
            // чтобы сервер точно получил OnAttentionExit
            ClearCurrentTarget();
            SetRaycasterActive(false);
        }
    }
}