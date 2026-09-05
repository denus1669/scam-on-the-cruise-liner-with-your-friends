using Blocks.Gameplay.Core;
using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Локальный индикатор удержания. 
    /// Слушает события от InteractionAddon и управляет 3D-прогресс-баром,
    /// который уже висит дочерним объектом на игроке.
    /// </summary>
    public class InteractionProgressIndicator : MonoBehaviour
    {
        [Header("Зависимости")]
        [Tooltip("Компонент взаимодействия игрока. Если пусто, попытается найти на этом или родительском объекте.")]
        [SerializeField] private InteractionAddon interactionAddon;

        [Tooltip("3D-шкала прогресса, дочерний объект игрока.")]
        [SerializeField] private ProgressBar3D progressBar;

        private void Awake()
        {
            // Автоматический поиск зависимостей, если они не заданы в инспекторе
            if (interactionAddon == null)
            {
                interactionAddon = GetComponentInParent<InteractionAddon>();
            }

            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<ProgressBar3D>(true);
            }

            // Прячем шкалу при старте
            HideProgressBar();
        }

        private void OnEnable()
        {
            if (interactionAddon != null)
            {
                interactionAddon.OnHoldProgress += HandleHoldProgress;
                interactionAddon.OnHoldCancelled += HandleHoldCancelled;
            }
        }

        private void OnDisable()
        {
            if (interactionAddon != null)
            {
                interactionAddon.OnHoldProgress -= HandleHoldProgress;
                interactionAddon.OnHoldCancelled -= HandleHoldCancelled;
            }
        }

        private void HandleHoldProgress(IInteractable target, float progress)
        {
            if (progressBar == null) return;

            // Включаем объект, если он был выключен
            if (!progressBar.gameObject.activeSelf)
            {
                progressBar.gameObject.SetActive(true);
            }

            // Обновляем значение
            progressBar.SetFill(progress);

            // Проверяем, требуется ли ожидание отпускания кнопки
            bool waitForRelease = false;
            if (target is IHoldReleaseInteractable holdRelease)
            {
                waitForRelease = holdRelease.WaitForRelease;
            }

            // Если заполнение достигло максимума И не нужно ждать отпускания — прячем шкалу
            if (progress >= 1f && !waitForRelease)
            {
                HideProgressBar();
            }
        }
        private void HandleHoldCancelled()
        {
            HideProgressBar();
        }

        private void HideProgressBar()
        {
            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(false);
                progressBar.SetFill(0f); // Сбрасываем визуал на 0 на всякий случай
            }
        }
    }
}