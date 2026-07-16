using UnityEngine;

/// <summary>
/// Контроллер 3D прогресс-бара недовольства бота.
/// Плавно анимирует заполнение и цвет шкалы в зависимости от текущего раздражения.
/// Работает только на клиенте (не на сервере).
/// </summary>
public class DispleasureBarController : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private ProgressBar3D progressBar;
    [SerializeField] private BotDispleasureController displeasureController;

    [Header("Настройки цветов")]
    [SerializeField] private Color lowDispleasureColor = Color.green;      // 0-33%
    [SerializeField] private Color mediumDispleasureColor = Color.yellow;  // 33-66%
    [SerializeField] private Color highDispleasureColor = Color.red;       // 66-100%

    [Header("Настройки анимации")]
    [SerializeField] private float fillAnimationSpeed = 5f;
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

    private float targetFillAmount = 0f;
    private float currentFillAmount = 0f;
    private float maxDispleasure = 100f;

    private void Awake()
    {
        if (progressBar == null)
        {
            progressBar = GetComponentInChildren<ProgressBar3D>();
            if (progressBar == null)
            {
                Debug.LogError($"[{GetType().Name}] ProgressBar3D не найден на {gameObject.name}");
                return;
            }
        }

        if (displeasureController == null)
        {
            displeasureController = GetComponentInParent<BotDispleasureController>();
            if (displeasureController == null)
            {
                Debug.LogError($"[{GetType().Name}] BotDispleasureController не найден на {gameObject.name}");
                return;
            }
        }

        maxDispleasure = displeasureController.MaxDispleasure;
    }

    private void OnEnable()
    {
        if (displeasureController != null)
        {
            displeasureController.OnDispleasureChanged += HandleDispleasureChanged;
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

    private void Update()
    {
        // Плавная анимация заполнения через Lerp
        currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * fillAnimationSpeed);
        progressBar.SetFill(currentFillAmount);
    }

    private void HandleDispleasureChanged(float displeasure)
    {
        targetFillAmount = Mathf.Clamp01(displeasure / maxDispleasure);
        Color fillColor = GetColorForDispleasure(displeasure);
        progressBar.SetColors(backgroundColor, fillColor);
    }

    /// <summary>
    /// Плавная интерполяция цвета в зависимости от уровня раздражения.
    /// </summary>
    private Color GetColorForDispleasure(float displeasure)
    {
        float normalizedDispleasure = displeasure / maxDispleasure;

        if (normalizedDispleasure < 0.33f)
        {
            float t = normalizedDispleasure / 0.33f;
            return Color.Lerp(lowDispleasureColor, mediumDispleasureColor, t);
        }
        else if (normalizedDispleasure < 0.66f)
        {
            float t = (normalizedDispleasure - 0.33f) / 0.33f;
            return Color.Lerp(mediumDispleasureColor, highDispleasureColor, t);
        }
        else
        {
            return highDispleasureColor;
        }
    }
}