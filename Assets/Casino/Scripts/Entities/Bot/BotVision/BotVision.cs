using UnityEngine;
using Unity.Netcode;

namespace Assets.Casino.Bot
{
    /// <summary>
    /// Серверный компонент зрения бота.
    /// Проверяет, находится ли цель в конусе зрения относительно головы бота.
    /// </summary>
    public class BotVision : NetworkBehaviour
    {
        [Header("Конфигурация")]
        [SerializeField] private BotVisionConfig config;

        [Header("Ссылки")]
        [Tooltip("Трансформ головы бота. От него считается направление конуса зрения.")]
        [SerializeField] private Transform head;

        [Header("Отладка")]
        [Tooltip("Рисовать линии зрения в рантайме через Debug.DrawLine")]
        [SerializeField] private bool drawDebugRuntime = true;

        /// <summary>
        /// Проверяет, видит ли бот точку в мировом пространстве.
        /// Проверка по горизонтальному углу и дистанции (Y игнорируется).
        /// </summary>
        public bool CanSeePoint(Vector3 worldPosition)
        {
            if (head == null || config == null) return false;

            Vector3 toTarget = worldPosition - head.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;

            // Проверка дистанции
            if (distance > config.viewDistance)
            {
                DrawDebugLine(worldPosition, false);
                return false;
            }

            if (toTarget.sqrMagnitude < 0.001f)
            {
                DrawDebugLine(worldPosition, true);
                return true; // Цель прямо на голове
            }

            Vector3 headForward = head.forward;
            headForward.y = 0f;

            if (headForward.sqrMagnitude < 0.001f)
            {
                DrawDebugLine(worldPosition, false);
                return false;
            }

            float angle = Vector3.Angle(headForward.normalized, toTarget.normalized);
            bool visible = angle <= config.viewAngle * 0.5f;

            DrawDebugLine(worldPosition, visible);
            return visible;
        }

        /// <summary>
        /// Проверяет, видит ли бот указанный трансформ.
        /// </summary>
        public bool CanSeeTransform(Transform target)
        {
            if (target == null) return false;
            return CanSeePoint(target.position);
        }

        private void DrawDebugLine(Vector3 targetPosition, bool visible)
        {
            if (!drawDebugRuntime || !IsServer) return;
            Color c = visible ? Color.green : Color.red;
            Debug.DrawLine(head.position, targetPosition, c, 0.25f);
        }

        // ---------- Гизмо для редактора ----------

        private void OnDrawGizmosSelected()
        {
            Transform origin = head != null ? head : transform;
            float angle = config != null ? config.viewAngle : 180f;
            float distance = config != null ? config.viewDistance : 10f;
            Color color = config != null ? config.debugColor : Color.yellow;
            DrawConeGizmo(origin, angle, distance, color);
        }

        private static void DrawConeGizmo(Transform origin, float angle, float distance, Color color)
        {
            Vector3 forward = origin.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();

            float halfAngle = angle * 0.5f;

            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * forward;

            Gizmos.color = color;
            Gizmos.DrawRay(origin.position, leftDir * distance);
            Gizmos.DrawRay(origin.position, rightDir * distance);

            // Дуга на конце конуса
            int segments = 16;
            Vector3 prev = origin.position + leftDir * distance;
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 dir = Quaternion.Euler(0, Mathf.Lerp(-halfAngle, halfAngle, t), 0) * forward;
                Vector3 curr = origin.position + dir * distance;
                Gizmos.DrawLine(prev, curr);
                prev = curr;
            }
        }
    }
}