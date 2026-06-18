using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Выполняет рейкаст из камеры игрока, когда активен режим внимания.
    /// Работает только для локального владельца.
    /// При попадании в объект с компонентом IAttentionTarget и коллайдером-триггером
    /// вызывает методы интерфейса, а также выводит информацию в консоль.
    /// </summary>
    public class AttentionRaycaster : NetworkBehaviour
    {
        [Header("Ссылки на компоненты")]
        [Tooltip("Контроллер внимания игрока, откуда берётся флаг активности.")]
        [SerializeField] private PlayerAttentionController attentionController;

        [Header("Настройки камеры")]
        [Tooltip("Камера, из которой будет выпускаться луч. Если не назначена, используется Camera.main.")]
        [SerializeField] private Camera mainCamera;

        [Header("Настройки луча")]
        [Tooltip("Слои, с которыми взаимодействует луч (только для объектов с IAttentionTarget и триггерами).")]
        [SerializeField] private LayerMask attentionLayerMask = ~0;
        [Tooltip("Максимальная дистанция луча.")]
        [SerializeField] private float maxRayDistance = 100f;

        // Текущая цель, на которую указывает луч
        private IAttentionTarget currentTarget;
        // Флаг активности режима внимания (нужен для управления корутиной)
        private bool isAttentionActive;

        #region Жизненный цикл Unity и Network

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Этот компонент активен только у локального игрока
            if (!IsOwner)
                return;

            // Получаем главную камеру, если не назначена вручную
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
            {
                Debug.LogError("[AttentionRaycaster] Главная камера не найдена.", this);
                return;
            }

            // Находим контроллер внимания, если ссылка не проставлена в инспекторе
            if (attentionController == null)
            {
                attentionController = GetComponent<PlayerAttentionController>();
                if (attentionController == null)
                {
                    Debug.LogError("[AttentionRaycaster] PlayerAttentionController не найден на объекте.", this);
                    return;
                }
            }

            // Подписываемся на изменение флага IsAttention
            attentionController.IsAttention.OnValueChanged += OnAttentionStateChanged;

            // Принудительно синхронизируем начальное состояние
            OnAttentionStateChanged(attentionController.IsAttention.Value, attentionController.IsAttention.Value);
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                // Отписываемся от события
                if (attentionController != null && attentionController.IsAttention != null)
                    attentionController.IsAttention.OnValueChanged -= OnAttentionStateChanged;

                // Останавливаем корутину и сбрасываем текущую цель
                StopAllCoroutines();
                ClearCurrentTarget();
            }
            base.OnNetworkDespawn();
        }

        #endregion

        #region Реакция на изменение режима внимания

        /// <summary>
        /// Вызывается при изменении значения NetworkVariable IsAttention.
        /// Запускает или останавливает цикл рейкаста.
        /// </summary>
        private void OnAttentionStateChanged(bool previousValue, bool newValue)
        {
            isAttentionActive = newValue;
            if (isAttentionActive)
            {
                StartCoroutine(RaycastLoop());
            }
            else
            {
                StopAllCoroutines();
                ClearCurrentTarget(); // При выходе из режима сбрасываем цель
            }
        }

        #endregion

        #region Основной цикл рейкаста

        /// <summary>
        /// Бесконечный цикл, работающий пока isAttentionActive == true.
        /// Каждый кадр выпускает луч и обрабатывает смену цели.
        /// </summary>
        private IEnumerator RaycastLoop()
        {
            while (isAttentionActive)
            {
                PerformRaycast();
                yield return null; // ждём следующий кадр
            }
        }

        /// <summary>
        /// Выпускает луч из mainCamera в направлении её взгляда.
        /// Если луч попадает в коллайдер-триггер на объекте с IAttentionTarget,
        /// обновляет текущую цель и вызывает соответствующие методы интерфейса.
        /// </summary>
        private void PerformRaycast()
        {
            if (mainCamera == null) return;

            Vector3 origin = mainCamera.transform.position;
            Vector3 direction = mainCamera.transform.forward;

            // Отрисовка для отладки (в Scene View)
            Debug.DrawRay(origin, direction * maxRayDistance, currentTarget != null ? Color.green : Color.red, 0.1f);

            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRayDistance, attentionLayerMask))
            {
                // Игнорируем коллайдеры, которые не являются триггерами
                if (!hit.collider.isTrigger)
                {
                    ClearCurrentTarget();
                    return;
                }

                // Пытаемся получить IAttentionTarget на объекте попадания (или его родителе)
                IAttentionTarget newTarget = hit.collider.GetComponentInParent<IAttentionTarget>();
                if (newTarget != currentTarget)
                {
                    ClearCurrentTarget();
                    currentTarget = newTarget;
                    if (currentTarget != null)
                    {
                        currentTarget.OnAttentionEnter(gameObject);
                        Debug.Log($"[Внимание] Луч попал в объект: {((Component)currentTarget).gameObject.name}");
                    }
                }
            }
            else
            {
                ClearCurrentTarget();
            }
        }

        /// <summary>
        /// Вызывает OnAttentionExit у текущей цели (если есть), сбрасывает её и логирует выход.
        /// </summary>
        private void ClearCurrentTarget()
        {
            if (currentTarget != null)
            {
                currentTarget.OnAttentionExit(gameObject);
                Debug.Log($"[Внимание] Луч покинул объект: {((Component)currentTarget).gameObject.name}");
                currentTarget = null;
            }
        }

        #endregion
    }
}