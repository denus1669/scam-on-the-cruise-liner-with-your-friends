using UnityEngine;

public class HierarchicalLookGlobal : MonoBehaviour
{
    [Header("Hierarchy Links")]
    [SerializeField] private Transform headSphere; // Сфера (голова) внутри этой группы

    [Header("Limits")]
    [Range(0f, 90f)][SerializeField] private float maxHeadX = 20f;     // Лимит наклона головы
    [Range(0f, 90f)][SerializeField] private float maxShouldersX = 30f; // Лимит наклона плеч

    private Transform cameraTransform;

    void Update()
    {
        // 1. Поиск камеры при старте
        if (cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            return;
        }

        // --- Получаем глобальные углы камеры ---
        float cameraYaw = cameraTransform.eulerAngles.y;   // Поворот Y (влево/вправо)
        float cameraPitch = cameraTransform.eulerAngles.x; // Наклон X (вверх/вниз)
        float cameraRoll = cameraTransform.eulerAngles.z;  // Крен Z (если нужен)

        // Нормализуем Pitch камеры в диапазон от -180 до 180
        if (cameraPitch > 180f) cameraPitch -= 360f;

        // --- Математика распределения ---

        // Общий максимальный угол, на который физически могут наклониться голова + плечи
        float totalMaxAngle = maxHeadX + maxShouldersX;

        // Ограничиваем наклон камеры рамками наших максимальных возможностей,
        // чтобы расчеты не ломались, если камера уйдет дальше (например, строго под ноги на 90 градусов)
        float clampedPitch = Mathf.Clamp(cameraPitch, -totalMaxAngle, totalMaxAngle);

        float headX = 0f;
        float shouldersX = 0f;

        if (totalMaxAngle > 0f) // Защита от деления на ноль
        {
            // 1. Находим "прогресс" наклона камеры от 0 до 1 (или от 0 до -1 для взгляда вверх)
            float pitchPercent = clampedPitch / totalMaxAngle;

            // 2. Голова поворачивается непрерывно на этот процент от своего максимума
            headX = pitchPercent * maxHeadX;

            // 3. Плечи работают по старой логике: подключаются только после прохождения порога головы
            float absolutePitch = Mathf.Abs(clampedPitch);
            if (absolutePitch > maxHeadX)
            {
                // Вычисляем знак наклона (1 - вниз, -1 - вверх)
                float sign = Mathf.Sign(clampedPitch);
                // Плечи забирают только то, что вышло за пределы лимита головы
                shouldersX = (absolutePitch - maxHeadX) * sign;
            }
        }

        // --- Применяем глобальные вращения ---

        // 1. Вращаем плечи (весь этот объект) в глобальном пространстве
        transform.rotation = Quaternion.Euler(shouldersX, cameraYaw, cameraRoll);

        // 2. Вращаем саму голову-сферу относительно плеч
        if (headSphere != null)
        {
            headSphere.rotation = transform.rotation * Quaternion.Euler(headX, 0f, 0f);
        }
    }
}