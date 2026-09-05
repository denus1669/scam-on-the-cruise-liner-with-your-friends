using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DeckZoneClickable : MonoBehaviour
{
    [Tooltip("Объект, который будет включаться при наведении (например, меш с outline-материалом)")]
    [SerializeField] private GameObject highlightVisual;

    private void OnMouseEnter()
    {
        if (highlightVisual != null)
            highlightVisual.SetActive(true);
    }

    private void OnMouseExit()
    {
        if (highlightVisual != null)
            highlightVisual.SetActive(false);
    }

    private void OnMouseDown()
    {
        // Скрываем подсветку при клике
        if (highlightVisual != null)
            highlightVisual.SetActive(false);

        // Напрямую обращаемся к менеджеру и говорим, что зона выбрана!
        if (DeckCutManager.Instance != null)
        {
            DeckCutManager.Instance.OnZoneSelected();
        }
        else
        {
            Debug.LogError("DeckCutManager не найден на сцене! Убедитесь, что он есть.");
        }
    }
}