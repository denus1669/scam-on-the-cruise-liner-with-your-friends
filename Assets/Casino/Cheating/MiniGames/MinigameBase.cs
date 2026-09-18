using Assets.Casino.UI;
using System;
using UnityEngine;

namespace Assets.Casino.Cheating.MiniGames
{
    public abstract class MinigameBase : MonoBehaviour
    {
        protected Action<bool> _onComplete;
        protected string _contextData;
        protected bool _isPlaying;

        [Header("Базовые настройки")]
        [SerializeField] protected GameObject panelRoot;

        [Header("Настройки курсора")]
        [SerializeField] protected bool manageCursor = true;

        [Header("Переключатель управления")]
        [Tooltip("Если не назначен — будет найден автоматически на сцене")]
        [SerializeField] private InputModeSwitcher inputModeSwitcher;

        private void Awake()
        {
            if (inputModeSwitcher == null)
                inputModeSwitcher = FindFirstObjectByType<InputModeSwitcher>();

            if (inputModeSwitcher == null)
                Debug.LogWarning("[MinigameBase] InputModeSwitcher не найден на сцене!", this);
        }

        public virtual void StartGame(string contextData, Action<bool> onComplete)
        {
            _contextData = contextData;
            _onComplete = onComplete;

            if (panelRoot != null)
                panelRoot.SetActive(true);

            inputModeSwitcher?.EnterUIMode(manageCursor);

            OnGameStarted();
        }

        protected virtual void OnGameStarted() { }

        protected void FinishGame(bool isSuccess)
        {
            inputModeSwitcher?.ExitUIMode(manageCursor);

            if (panelRoot != null)
                panelRoot.SetActive(false);

            _onComplete?.Invoke(isSuccess);
            _onComplete = null;
        }

        public virtual void ForceClose()
        {
            inputModeSwitcher?.ExitUIMode(manageCursor);

            if (panelRoot != null)
                panelRoot.SetActive(false);

            _onComplete = null;
            _isPlaying = false;
        }

        public virtual void WinGame()
        {
            Debug.Log("<color=green>Выигрыш</color>");
            FinishGame(true);
        }

        public virtual void LoseGame()
        {
            Debug.Log("<color=red>Проигрыш</color>");
            FinishGame(false);
        }
    }
}