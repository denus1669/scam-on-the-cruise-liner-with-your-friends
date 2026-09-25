public class HighlighterInteractable : HighlightControllerBase
{
    private void Reset()
    {
        _interactable = GetComponent<InteractableBase>();
    }

    private void OnEnable()
    {
        if (_interactable == null) _interactable = GetComponent<InteractableBase>();

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