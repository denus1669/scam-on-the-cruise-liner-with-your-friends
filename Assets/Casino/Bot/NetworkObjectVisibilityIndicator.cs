using UnityEngine;

/// <summary>
/// Базовый класс для индикаторов подозрительного поведения бота.
/// Управляет MeshRenderer'ом заданного объекта (включение/выключение).
/// Конкретные наследники только подписываются на нужное событие контроллера.
/// </summary>
public abstract class NetworkObjectVisibilityIndicator : MonoBehaviour
{
    [Header("Визуал")]
    [Tooltip("Объект, MeshRenderer которого будет включаться/выключаться.")]
    [SerializeField] private GameObject targetObject;

    protected MeshRenderer MeshRenderer { get; private set; }

    protected virtual void Awake()
    {
        if (targetObject == null)
        {
            Debug.LogWarning($"[{GetType().Name}] targetObject не назначен на {gameObject.name}");
            return;
        }

        MeshRenderer = targetObject.GetComponent<MeshRenderer>();
        if (MeshRenderer == null)
        {
            Debug.LogWarning($"[{GetType().Name}] На {targetObject.name} отсутствует MeshRenderer!");
            return;
        }

        HideIndicator();
    }

    protected void ShowIndicator()
    {
        if (MeshRenderer != null)
            MeshRenderer.enabled = true;
    }

    protected void HideIndicator()
    {
        if (MeshRenderer != null)
            MeshRenderer.enabled = false;
    }

    /// <summary>
    /// Универсальный обработчик для bool-событий контроллеров.
    /// Наследники передают его как callback при подписке.
    /// </summary>
    protected void HandleStateChanged(bool isActive)
    {
        if (isActive) ShowIndicator();
        else HideIndicator();
    }
}
