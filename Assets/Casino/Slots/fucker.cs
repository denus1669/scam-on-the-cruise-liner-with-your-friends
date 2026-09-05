using UnityEngine;

public class CopyCameraRotation : MonoBehaviour
{
    private Transform cameraTransform;

    void Update()
    {
        // ≈сли камера еще не найдена (например, в первые секунды после старта сцены)
        if (cameraTransform == null)
        {
            // »щем главную камеру, к которой прив€зан Cinemachine
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            return; // ѕропускаем кадр, пока камера не по€витс€
        }

        // ѕереносим вращение камеры на шар
        transform.rotation = cameraTransform.rotation;
    }
}