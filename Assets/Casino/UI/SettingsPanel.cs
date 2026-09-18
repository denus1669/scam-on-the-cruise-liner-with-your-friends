using System;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Casino.UI
{
    public class SettingsPanel : MonoBehaviour
    {
        public event Action OnAudioPressed;
        public event Action OnInputPressed;
        public event Action OnBackPressed;

        [SerializeField] private Button audioButton;
        [SerializeField] private Button inputButton;
        [SerializeField] private Button backButton;

        private void OnEnable()
        {
            audioButton?.onClick.AddListener(() => OnAudioPressed?.Invoke());
            inputButton?.onClick.AddListener(() => OnInputPressed?.Invoke());
            backButton?.onClick.AddListener(() => OnBackPressed?.Invoke());
        }

        private void OnDisable()
        {
            audioButton?.onClick.RemoveAllListeners();
            inputButton?.onClick.RemoveAllListeners();
            backButton?.onClick.RemoveAllListeners();
        }
    }
}