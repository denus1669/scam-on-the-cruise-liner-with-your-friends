using UnityEngine;

/// <summary>
/// Отвечает исключительно за визуальное представление карты (View).
/// Адаптировано для 3D-мира (использует SpriteRenderer вместо Image).
/// </summary>
public class CardView : MonoBehaviour
{
    [Header("Компоненты отрисовки (перетащить из префаба)")]
    [SerializeField] private SpriteRenderer faceRenderer;       // Для лица/масти карты
    [SerializeField] private SpriteRenderer backgroundRenderer; // Для фона/рубашки карты

    [Header("Настройки размера")]
    [SerializeField, Tooltip("Желаемые ширина и высота карты в 3D мире (в метрах)")]
    private Vector2 targetSize = new Vector2(0.02529306f, 0.0269854f);
    [SerializeField, Tooltip("Автоматически подгонять масштаб спрайта")]
    private bool autoScaleToFit = true;

    [Header("Настройки Transform")]
    [SerializeField] private float targetYFacePosition = 0.0085f;
    [SerializeField] private float targetYBackPosition = 0.0001f;
    [SerializeField] private float targetXRotation = 90;

    [Header("Спрайты по умолчанию (задаются в Инспекторе)")]
    [SerializeField] private Sprite defaultFaceSprite;
    [SerializeField] private Sprite defaultBackgroundSprite;

    private void Start()
    {
        if (defaultFaceSprite != null) SetFaceSprite(defaultFaceSprite);
        if (defaultBackgroundSprite != null) SetBackgroundSprite(defaultBackgroundSprite);
    }

    public void SetVisuals(Sprite faceSprite, Sprite bgSprite)
    {
        SetFaceSprite(faceSprite);
        SetBackgroundSprite(bgSprite);
    }

    public void SetFaceSprite(Sprite sprite)
    {
        if (faceRenderer != null)
        {
            faceRenderer.sprite = sprite;
            if (autoScaleToFit && sprite != null)
            {
                FitSpriteToSize(faceRenderer);
                SetXRotation(faceRenderer);
                SetYPositionFace(faceRenderer);
            }
        }
        else
            Debug.LogWarning("Компонент faceRenderer не назначен в CardView!");
    }

    public void SetBackgroundSprite(Sprite sprite)
    {
        if (backgroundRenderer != null)
        {
            backgroundRenderer.sprite = sprite;
            if (autoScaleToFit && sprite != null)
            { 
                FitSpriteToSize(backgroundRenderer);
                SetXRotation(backgroundRenderer);
                SetXPositionBack(backgroundRenderer);
            }
        }
        else
            Debug.LogWarning("Компонент backgroundRenderer не назначен в CardView!");
    }

    // Метод, который сжимает или растягивает объект со спрайтом под нужный размер
    private void FitSpriteToSize(SpriteRenderer renderer)
    {
        // 1. Узнаем размер текущего спрайта в Unity-юнитах
        Vector2 spriteSize = renderer.sprite.bounds.size;

        // 2. Высчитываем коэффициент масштабирования
        Vector3 newScale = new Vector3(
            targetSize.x / spriteSize.x,
            targetSize.y / spriteSize.y,
            1f // По Z плоскую картинку не масштабируем
        );

        // 3. Применяем масштаб к Transform (пустышке Face или Background)
        renderer.transform.localScale = newScale;
    }

    private void SetXRotation(SpriteRenderer renderer)
    {
        renderer.transform.rotation = Quaternion.Euler(targetXRotation, 0, 0);

    }

    private void SetYPositionFace(SpriteRenderer renderer)
    {
        renderer.transform.localPosition = new Vector3(0, targetYFacePosition, 0);
    }

    private void SetXPositionBack(SpriteRenderer renderer)
    {
        renderer.transform.localPosition = new Vector3(0, targetYBackPosition, 0);
    }
    
}