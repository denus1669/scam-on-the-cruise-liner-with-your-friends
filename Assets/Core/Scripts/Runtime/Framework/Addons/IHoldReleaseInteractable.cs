using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Расширение интерфейса IInteractable для объектов, 
    /// которые умеют реагировать на досрочное отпускание кнопки (зарядка удара/броска).
    /// </summary>
    public interface IHoldReleaseInteractable : IInteractable
    {
        /// <summary>
        /// Вызывается из InteractionAddon, когда игрок отпускает кнопку до того, 
        /// как заполнится HoldDuration.
        /// </summary>
        /// <param name="interactor">Игрок, который отпустил кнопку</param>
        /// <param name="chargeTime">Время в секундах, которое кнопка была зажата</param>
        void OnHoldReleased(GameObject interactor, float chargeTime);

        void OnHoldStarted(GameObject interactor);

        /// <summary>
        /// Если true, взаимодействие Interact() не вызовется автоматически по таймеру.
        /// Система будет ждать отпускания кнопки (OnHoldReleased).
        /// </summary>
        bool WaitForRelease { get; }
    }
}
