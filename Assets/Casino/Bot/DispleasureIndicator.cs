using UnityEngine;

/// <summary>
/// Индикатор недовольства бота.
/// Показывает визуальный маркер (например, "!" над головой) только когда раздражение превышает порог.
/// Наследуется от базового класса, управляющего MeshRenderer'ом.
/// </summary>
public class DispleasureIndicator : NetworkObjectVisibilityIndicator
{
    [Header("Настройки появления")]
    [Tooltip("Минимальный уровень раздражения, при котором индикатор становится видимым")]
    [SerializeField] private float minimumDispleasureToShow = 1f;

    private BotDispleasureController displeasureController;

    protected override void Awake()
    {
        // ВАЖНО: сначала вызываем базовый Awake для инициализации MeshRenderer
        base.Awake();

        displeasureController = GetComponentInParent<BotDispleasureController>();
        if (displeasureController == null)
        {
            Debug.LogWarning($"[{GetType().Name}] BotDispleasureController не найден на родителе {gameObject.name}");
        }
    }

    private void OnEnable()
    {
        if (displeasureController != null)
        {
            displeasureController.OnDispleasureChanged += HandleDispleasureChanged;

            // Первичная проверка состояния
            HandleDispleasureChanged(displeasureController.CurrentDispleasure);
        }
    }

    private void OnDisable()
    {
        if (displeasureController != null)
        {
            displeasureController.OnDispleasureChanged -= HandleDispleasureChanged;
        }
    }

    private void HandleDispleasureChanged(float displeasure)
    {
        // Преобразуем float в bool через пороговое значение
        bool shouldShow = displeasure >= minimumDispleasureToShow;
        HandleStateChanged(shouldShow);
    }
}