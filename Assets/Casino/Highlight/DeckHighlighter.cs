using Assets.Casino.Interactable;
using UnityEngine;

[RequireComponent(typeof(DeckInteractable))]
public class DeckHighlighter : HighlightControllerBase
{
    private void Reset()
    {
        _interactable = GetComponent<DeckInteractable>();
    }

    private void OnEnable()
    {
        if (_interactable == null) _interactable = GetComponent<DeckInteractable>();

        // Подписываемся на изменения
        _interactable.OnAvailabilityChanged += SetAvailableHighlight;

        // Синхронизируем начальное состояние
        SetAvailableHighlight(_interactable.HasAnyAvailableInteractor());
    }

    private void OnDisable()
    {
        if (_interactable != null)
            _interactable.OnAvailabilityChanged -= SetAvailableHighlight;
    }

}