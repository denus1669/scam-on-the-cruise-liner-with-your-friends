using UnityEngine;

[ExecuteAlways]
public class ProgressBar3D : MonoBehaviour
{
    [Header("Настройки шкалы")]
    [Tooltip("Значение заполнения от 0 (пусто) до 1 (полно)")]
    [Range(0f, 1f)]
    public float fillAmount = 0.5f;

    [Header("Цвета шкалы")]
    public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f); // Темно-серый по умолчанию
    public Color fillColor = Color.green; // Зеленый по умолчанию

    [Header("Тестирование (работает в Play Mode)")]
    public bool testAnimation = false;
    public float animationSpeed = 1f;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;

    // Кэшируем ID всех параметров для максимальной производительности
    private static readonly int FillAmountProp = Shader.PropertyToID("_FillAmount");
    private static readonly int BgColorProp = Shader.PropertyToID("_BackgroundColor");
    private static readonly int FillColorProp = Shader.PropertyToID("_FillColor");

    void Start()
    {
        UpdateFill();
    }

    void Update()
    {
        if (Application.isPlaying && testAnimation)
        {
            fillAmount = Mathf.PingPong(Time.time * animationSpeed, 1f);
            UpdateFill();
        }
    }

    private void OnValidate()
    {
        UpdateFill();
    }

    // =========================================================
    // БАЗА ДЛЯ ДЕНИСА .!.
    // =========================================================
    public void SetFill(float value)
    {
        fillAmount = Mathf.Clamp01(value);
        UpdateFill();
    }

    // Новый метод для Дениса, если он захочет менять цвет кодом (например, шкала краснеет при малом ХП)
    public void SetColors(Color background, Color fill)
    {
        backgroundColor = background;
        fillColor = fill;
        UpdateFill();
    }

    private void UpdateFill()
    {
        if (_renderers == null || _renderers.Length == 0)
            _renderers = GetComponentsInChildren<Renderer>();

        if (_propBlock == null)
            _propBlock = new MaterialPropertyBlock();

        foreach (var rnd in _renderers)
        {
            if (rnd == null) continue;

            rnd.GetPropertyBlock(_propBlock);

            // Передаем значения в шейдер
            _propBlock.SetFloat(FillAmountProp, fillAmount);
            _propBlock.SetColor(BgColorProp, backgroundColor);
            _propBlock.SetColor(FillColorProp, fillColor);

            rnd.SetPropertyBlock(_propBlock);
        }
    }
}