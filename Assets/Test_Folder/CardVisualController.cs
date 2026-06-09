using UnityEngine;

public class CardVisualController : MonoBehaviour
{
    [Header("Настройки лица")]
    public Vector2 faceIndex;       // Координаты номинала (X, Y)

    [Header("Настройки для мультиплеера")]
    public bool isVisible = true;    // Чекбокс: включено — видим лицо, выключено — заглушку
    public Vector2 placeholderIndex; // Индекс заглушки для врагов

    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;

    // Кэшируем ID свойств шейдера
    private static readonly int FaceId = Shader.PropertyToID("_FaceIndex");
    private static readonly int VisibleId = Shader.PropertyToID("_IsVisible");
    private static readonly int PlaceholderId = Shader.PropertyToID("_PlaceholderIndex");

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        UpdateCardVisuals();
    }

    // Обновление в реальном времени при изменении в инспекторе
    void OnValidate()
    {
        UpdateCardVisuals();
    }

    public void UpdateCardVisuals()
    {
        if (_renderer == null) _renderer = GetComponent<Renderer>();
        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

        _renderer.GetPropertyBlock(_propBlock);

        // Устанавливаем координаты
        _propBlock.SetVector(FaceId, faceIndex);
        _propBlock.SetVector(PlaceholderId, placeholderIndex);

        // Конвертируем bool в float (true = 1.0, false = 0.0)
        _propBlock.SetFloat(VisibleId, isVisible ? 1f : 0f);

        _renderer.SetPropertyBlock(_propBlock);
    }
}