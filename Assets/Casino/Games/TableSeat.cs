using UnityEngine;

/// <summary>
/// Интерактивный объект "Место за столом". 
/// При взаимодействии телепортирует игрока в заданную позицию (позицию дилера).
/// </summary>
public class TableSeat : MonoBehaviour, IInteractable
{
    [Header("Настройки позиции")]
    [SerializeField, Tooltip("Точка (пустышка), куда будет перемещен игрок")]
    private Transform standPosition;

    /// <summary>
    /// Телепортирует игрока в точку standPosition.
    /// </summary>
    /// <param name="interactor">Объект игрока.</param>
    public void Interact(GameObject interactor)
    {
        // Если у игрока есть CharacterController, его нужно отключить перед телепортацией,
        // иначе физика Unity вернет его на старое место.
        CharacterController cc = interactor.GetComponent<CharacterController>();

        if (cc != null) cc.enabled = false;

        // Перемещаем и поворачиваем игрока
        interactor.transform.position = standPosition.position;
        interactor.transform.rotation = standPosition.rotation;

        if (cc != null) cc.enabled = true;

        Debug.Log("Игрок занял место за столом!");
    }

    /// <summary>
    /// Подсказка для UI.
    /// </summary>
    public string GetInteractPrompt()
    {
        return "Встать за стол (E)";
    }
}