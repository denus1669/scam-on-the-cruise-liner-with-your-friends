using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Core
{
    /// <summary>
    /// Привязывает Yaw (горизонтальный поворот) префаба игрока к камере.
    /// Работает только на владельце (IsOwner). NetworkTransform сам
    /// реплицирует этот поворот другим клиентам.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerCameraYawLock : NetworkBehaviour
    {
        private Transform _cameraTransform;

        private void LateUpdate()
        {
            // Работаем только у владельца игрока
            if (!IsOwner) return;

            // Ленивая инициализация камеры
            if (_cameraTransform == null)
            {
                if (Camera.main != null)
                    _cameraTransform = Camera.main.transform;
                else
                    return;
            }

            // Берём только Yaw камеры, сохраняем текущий X/Z (на случай наклона префаба)
            float yaw = _cameraTransform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}