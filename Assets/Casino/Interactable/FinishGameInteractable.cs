using Unity.Netcode;
using UnityEngine;
using Assets.Casino.Games.BlackGreg;

namespace Assets.Casino.Games
{
    /// <summary>
    /// Интерактивный объект для завершения игры.
    /// Двухступенчатая система:
    /// 1. Удержание 2 сек → вскрыть карты (RevealHands)
    /// 2. Мгновенно → завершить партию (FinishGame)
    /// </summary>
    public class FinishGameInteractable : InteractableBase
    {
        [Header("Стол")]
        [SerializeField] private BlackGregTable blackGregTable;

        [Header("Настройки взаимодействия")]
        [SerializeField] private InteractionTriggerMode triggerMode = InteractionTriggerMode.OnButtonPress;
        [SerializeField] private int priority = 5;

        [SerializeField] private float _timeToHold = 1f;

        // Кэш для оптимизации обновления текста подсказки
        private string m_CachedPrompt;
        private bool m_LastGameStarted;
        private bool m_LastCanReveal;
        private bool m_LastCanFinish;

        public override InteractionTriggerMode TriggerMode => triggerMode;
        public override int Priority => priority;

        /// <summary>
        /// Динамическое время удержания: 2 сек для Reveal, 0 сек для Finish.
        /// </summary>
        public override float HoldDuration => blackGregTable != null && blackGregTable.IsRevealed ? 0f : _timeToHold;

        /// <summary>
        /// Динамический текст подсказки.
        /// </summary>
        public override string InteractionPromptText
        {
            get
            {
                bool gameStarted = blackGregTable != null && blackGregTable.IsGameStarted;
                bool canReveal = blackGregTable != null && blackGregTable.CanPlayerReveal();
                bool canFinish = blackGregTable != null && blackGregTable.CanPlayerFinish();

                if (gameStarted != m_LastGameStarted || canReveal != m_LastCanReveal ||
                    canFinish != m_LastCanFinish || m_CachedPrompt == null)
                {
                    m_LastGameStarted = gameStarted;
                    m_LastCanReveal = canReveal;
                    m_LastCanFinish = canFinish;

                    m_CachedPrompt = blackGregTable == null ? "Стол недоступен" :
                        !gameStarted ? "Игра не началась" :
                        canReveal ? "Вскрыть карты (Удерживайте E)" :
                        //canFinish ? "Завершить партию (E)" :
                        "Бот не закончил ходить или Игрок не взял 2 карты";
                }

                return m_CachedPrompt;
            }
        }

        /// <summary>
        /// Определяет, может ли объект быть в фокусе.
        /// </summary>
        public override bool CanInteract(GameObject interactor)
        {
            if (blackGregTable == null || !blackGregTable.IsOccupied || !blackGregTable.IsGameStarted)
                return false;

            if (!interactor.TryGetComponent<NetworkObject>(out var netObj))
                return false;

            // Только владелец стола может взаимодействовать
            return netObj.OwnerClientId == blackGregTable.OccupiedByClientId;
        }

        /// <summary>
        /// Выполняется после успешного удержания/нажатия кнопки.
        /// </summary>
        public override void Interact(GameObject interactor)
        {
            if (!IsSpawned || blackGregTable == null)
                return;

            ulong clientId = interactor.GetComponent<NetworkObject>().OwnerClientId;

            if (blackGregTable.CanPlayerReveal())
            {
                blackGregTable.RevealHandsServerRpc(clientId);
            }

            /*
            else if (blackGregTable.CanPlayerFinish())
            {
                blackGregTable.FinishGameServerRpc(clientId);
            }*/
        }

        /// <summary>
        /// Возвращает true, если хотя бы один игрок сейчас может взаимодействовать с объектом завершения игры.
        /// Используется подсветкой доступности.
        /// </summary>
        public override bool HasAnyAvailableInteractor()
        {
            // Стол должен быть занят и игра должна быть начата
            if (blackGregTable == null || !blackGregTable.IsOccupied || !blackGregTable.IsGameStarted)
                return false;

            // Если условия выполнены, владелец стола может взаимодействовать
            return true;
        }
    }
}