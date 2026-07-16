using UnityEngine;
using TMPro;
using Blocks.Gameplay.Core; // Подключаем TextMeshPro

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Локальный UI-компонент, который слушает InteractionAddon 
    /// и показывает/скрывает подсказку на экране.
    /// Можно повесить на Canvas или префаб UI игрока.
    /// </summary>
    public class InteractionUIController : MonoBehaviour
    {
        [Header("Настройки UI")]
        [Tooltip("Ссылка на текстовый компонент (TextMeshPro), который будет показывать подсказку.")]
        [SerializeField] private TextMeshProUGUI promptText;

        [Tooltip("Объект (например, панель или иконка), который мы будем включать/выключать целиком.")]
        [SerializeField] private GameObject promptPanel;

        [Header("Ссылка на Аддон")]
        [Tooltip("Аддон взаимодействия локального игрока (откуда мы берем события).")]
        [SerializeField] private InteractionAddon interactionAddon;

        private void Awake()
        {
            // Убедимся, что при старте UI скрыт
            HidePrompt();
        }

        private void OnEnable()
        {
            if (interactionAddon != null)
            {
                interactionAddon.OnFocusEnterLocal += ShowPrompt;
                interactionAddon.OnFocusExitLocal += HidePrompt;
            }
        }

        private void OnDisable()
        {
            if (interactionAddon != null)
            {
                interactionAddon.OnFocusEnterLocal -= ShowPrompt;
                interactionAddon.OnFocusExitLocal -= HidePrompt;
            }
        }

        /// <summary>
        /// Показывает текст, когда игрок смотрит на объект
        /// </summary>
        private void ShowPrompt(IInteractable interactable)
        {
            if (promptPanel != null) promptPanel.SetActive(true);

            if (promptText != null)
            {
                // В идеале: если в вашем интерфейсе IInteractable есть свойство (например, PromptMessage), 
                // можно писать: promptText.text = interactable.PromptMessage;
                // Но пока ставим стандартный текст:
                promptText.text = interactable.InteractionPromptText;
            }
        }

        /// <summary>
        /// Скрывает текст, когда объект пропадает из фокуса
        /// </summary>
        private void HidePrompt()
        {
            if (promptPanel != null) promptPanel.SetActive(false);

            // Если панели нет, можно просто очищать текст
            if (promptText != null && promptPanel == null)
            {
                promptText.text = string.Empty;
            }
        }
    }
}