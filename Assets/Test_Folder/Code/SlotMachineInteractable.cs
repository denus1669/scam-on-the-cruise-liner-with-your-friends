using UnityEngine;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Компонент игрового автомата, поддерживающий механику случайных поломок и интеграцию с CoreInteractor.
    /// </summary>
    public class SlotMachineInteractable : MonoBehaviour, IInteractable
    {
        [Header("Настройки автомата")]
        [SerializeField] private string machineName = "Slot Machine";
        [SerializeField] private int interactionPriority = 10;

        private bool m_IsBroken = false;
        private SlotMachineBreakdownManager m_Manager;

        // ==========================================
        // Реализация интерфейса IInteractable
        // ==========================================

        public InteractionTriggerMode TriggerMode => InteractionTriggerMode.OnButtonPress;
        public int Priority => interactionPriority;

        // ДОБАВЛЕНО: Реализация недостающего члена интерфейса.
        // Так как тип возвращаемого значения в вашем интерфейсе может быть float, возвращаем 0f.
        // Если интерфейс требует другой тип (например, int), измените тип данных здесь.
        public float HoldDuration => 0f;

        // Текст подсказки для UI (показывается только если автомат сломан)
        public string InteractionPromptText => m_IsBroken ? $"Нажмите E, чтобы починить {machineName}" : string.Empty;

        // ==========================================

        public bool IsBroken => m_IsBroken;
        public string MachineName => machineName;

        /// <summary>
        /// Инициализация связи с менеджером поломок.
        /// </summary>
        public void Initialize(SlotMachineBreakdownManager manager)
        {
            m_Manager = manager;
            m_IsBroken = false;
        }

        /// <summary>
        /// Вызывается менеджером, когда наступает время поломки этого автомата.
        /// </summary>
        public void TriggerBreakdown()
        {
            if (m_IsBroken) return;

            m_IsBroken = true;
            Debug.Log($"<color=red>[ПОЛОМКА]</color> Игровой автомат '{machineName}' сломался! Требуется починка.");
        }

        /// <summary>
        /// Проверка системы взаимодействия: можно ли взаимодействовать с объектом прямо сейчас.
        /// </summary>
        public bool CanInteract(GameObject interactor)
        {
            // Взаимодействовать можно только в том случае, если автомат сломан
            return m_IsBroken;
        }

        /// <summary>
        /// Основная логика взаимодействия, вызываемая при нажатии игроком клавиши 'E'.
        /// </summary>
        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;

            FixMachine();
        }

        private void FixMachine()
        {
            m_IsBroken = false;
            Debug.Log($"<color=green>[ПОЧИНКА]</color> Игровой автомат '{machineName}' успешно починен игроком!");

            if (m_Manager != null)
            {
                m_Manager.OnMachineFixed(this);
            }
        }
    }
}