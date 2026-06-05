/*

using UnityEngine;

/// <summary>
/// Интерактивный объект "Кнопка Готовности / Звонок".
/// Сигнализирует о том, что игрок закончил добор карт. 
/// Когда и игрок, и бот нажали готовность - карты вскрываются.
/// </summary>
public class ReadyButtonInteractable : MonoBehaviour, IInteractable
{
    // === ДЛЯ ПЕРЕХОДА НА InteractionAddon.cs РАСКОММЕНТИРУЙ ЭТИ СТРОКИ: ===
    // public InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;
    // public int Priority => 1;
    // public bool CanInteract(GameObject interactor) => true;
    // ======================================================================

    [Header("Связи")]
    [SerializeField, Tooltip("Ссылка на менеджер этого стола")]
    private BlackGregManager tableManager;

    /// <summary>
    /// Переключает статус готовности игрока.
    /// </summary>
    /// <param name="interactor">Объект игрока.</param>
    public void Interact(GameObject interactor)
    {
        if (tableManager != null)
        {
            tableManager.TogglePlayerReady();
        }
        else
        {
            Debug.LogError("Кнопке Готовности не назначен BlackGregManager!");
        }
    }

    /// <summary>
    /// Подсказка для UI.
    /// </summary>
    public string GetInteractPrompt()
    {
        return "Вскрыть карты (E)";
    }
}

*/