using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Компонент Игрока. Пускает луч (Raycast) из камеры в центр экрана.
/// Ищет объекты с интерфейсом IInteractable и обрабатывает нажатие кнопки взаимодействия.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("Настройки луча")]
    [SerializeField, Tooltip("Камера от первого лица")]
    private Camera playerCamera;

    [SerializeField, Tooltip("Максимальная дистанция взаимодействия")]
    private float interactRange = 3f;

    [SerializeField, Tooltip("Слои, с которыми пересекается луч (чтобы не кликать сквозь стены)")]
    private LayerMask interactLayerMask;

    // В будущем здесь можно добавить ссылку на UI текст, чтобы выводить подсказки на экран
    // private TextMeshProUGUI promptTextUI;

    private IInteractable currentInteractable;

    private void Update()
    {
        CheckForInteractable();
        HandleInteractionInput();
    }

    /// <summary>
    /// Пускает луч из центра экрана и проверяет, есть ли перед нами интерактивный объект.
    /// </summary>
    private void CheckForInteractable()
    {
        // Пускаем луч из центра камеры вперед
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hitInfo, interactRange, interactLayerMask))
        {
            // Пытаемся получить интерфейс IInteractable с объекта, в который попали
            IInteractable interactable = hitInfo.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                currentInteractable = interactable;
                // Здесь в будущем можно выводить currentInteractable.GetInteractPrompt() на экран
                return;
            }
        }

        // Если луч никуда не попал или попал в неинтерактивный объект
        currentInteractable = null;
    }

    /// <summary>
    /// Обрабатывает нажатие клавиши взаимодействия (E).
    /// </summary>
    private void HandleInteractionInput()
    {
        // ВАЖНО: Используй новую систему ввода (Input Action Asset) в будущем, 
        // пока для прототипа оставляем прямое чтение Keyboard для простоты
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (currentInteractable != null)
            {
                // Передаем gameObject игрока, чтобы интерактивный объект знал, кого телепортировать или кому давать фишки
                currentInteractable.Interact(this.gameObject);
            }
        }
    }
}