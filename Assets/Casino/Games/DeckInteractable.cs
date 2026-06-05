using UnityEngine;

/// <summary>
/// Интерактивный объект "Колода карт".
/// При взаимодействии дает команду столу выдать карту игроку.
/// </summary>
public class DeckInteractable : MonoBehaviour, IInteractable
{
    [Header("Связи")]
    [SerializeField, Tooltip("Ссылка на менеджер этого стола")]
    private BlackGregManager tableManager;

    /// <summary>
    /// Дает команду столу выдать карту.
    /// </summary>
    /// <param name="interactor">Игрок, который берет карту.</param>
    public void Interact(GameObject interactor)
    {
        if (tableManager != null)
        {
            tableManager.PlayerDrawCard();
        }
        else
        {
            Debug.LogError("В колоде не назначен BlackGregManager!");
        }
    }

    /// <summary>
    /// Подсказка для UI.
    /// </summary>
    public string GetInteractPrompt()
    {
        return "Взять карту (E)";
    }
}