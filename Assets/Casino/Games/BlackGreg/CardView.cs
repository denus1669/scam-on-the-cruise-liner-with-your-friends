using UnityEngine;

/// <summary>
/// Скрипт, который вешается на префаб карты. 
/// Выступает мостом между логикой менеджера и визуальным контроллером шейдера.
/// </summary>
[RequireComponent(typeof(CardVisualController))]
public class CardView : MonoBehaviour
{
    private CardVisualController _visualController;
    private CardData _currentData;

    private void Awake()
    {
        _visualController = GetComponent<CardVisualController>();
    }

    /// <summary>
    /// Метод вызывается из BlackGregManager при спавне карты.
    /// </summary>
    public void SetCardData(CardData data)
    {
        _currentData = data;

        // 1. Получаем правильный Vector2 из словаря
        Vector2 newFaceIndex = CardVisualMapper.GetFaceIndex(data);

        // 2. Устанавливаем его в контроллер шейдера
        if (_visualController == null) _visualController = GetComponent<CardVisualController>();

        _visualController.faceIndex = newFaceIndex;

        // 3. Принудительно обновляем материал
        _visualController.UpdateCardVisuals();
    }

    /// <summary>
    /// Меняет видимость карты (лицо или рубашка/заглушка)
    /// </summary>
    public void SetVisible(bool isVisible)
    {
        if (_visualController == null) _visualController = GetComponent<CardVisualController>();

        _visualController.isVisible = isVisible;
        _visualController.UpdateCardVisuals();
    }

    public CardData GetCardData()
    {
        return _currentData;
    }
}