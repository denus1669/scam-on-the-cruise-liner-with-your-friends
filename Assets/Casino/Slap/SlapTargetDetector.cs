using Blocks.Gameplay.Core;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Отвечает ТОЛЬКО за поиск цели для шлепка с помощью рейкаста.
/// Ничего не знает о сети, анимациях или инпуте.
/// </summary>
public class SlapTargetDetector : MonoBehaviour
{
    [Header("Настройки поиска цели")]
    [SerializeField] private float slapRange = 2.5f;
    [SerializeField] private LayerMask slapLayerMask = ~0;
    [Tooltip("Как часто пускать луч (в секундах). Оптимизация CPU")]
    [SerializeField] private float raycastInterval = 0.1f;

    [Header("Точка пуска луча")]
    [Tooltip("Откуда пускаем луч. Если пусто - берется Camera.main")]
    [SerializeField] private Transform raycastOrigin;

    public ISlapTarget CurrentTarget { get; private set; }
    public bool HasTarget => CurrentTarget != null;

    private float _nextRaycastTime;

    private void Update()
    {
        // Оптимизация: пускаем луч не каждый кадр
        if (Time.time >= _nextRaycastTime)
        {
            DetectTarget();
            _nextRaycastTime = Time.time + raycastInterval;
        }
    }

    private void DetectTarget()
    {
        Vector3 origin = raycastOrigin != null ? raycastOrigin.position : (Camera.main != null ? Camera.main.transform.position : transform.position);
        Vector3 direction = raycastOrigin != null ? raycastOrigin.forward : (Camera.main != null ? Camera.main.transform.forward : transform.forward);

        if (Physics.Raycast(origin, direction, out RaycastHit hit, slapRange, slapLayerMask))
        {
            // Ищем ISlapTarget на самом коллайдере или ВЫШЕ по иерархии
            CurrentTarget = hit.collider.GetComponentInParent<ISlapTarget>();
        }
        else
        {
            CurrentTarget = null;
        }
    }
}