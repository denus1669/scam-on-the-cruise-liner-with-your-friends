using UnityEngine;
using TMPro;
using Unity.Netcode;
using Blocks.Gameplay.Core;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Клиентский UI-компонент для отображения баланса кассы и всплывающих уведомлений.
    /// Висит на Canvas префаба игрока. Работает ТОЛЬКО для локального владельца (IsOwner).
    /// </summary>
    public class BankUI : NetworkBehaviour
    {
        [Header("Зависимости (автоматически находятся в сцене при спавне)")]
        [SerializeField] private CasinoBank casinoBank;
        [SerializeField] private BankVfxNotifier vfxNotifier;

        [Header("UI элементы")]
        [SerializeField] private TextMeshProUGUI balanceText;
        [SerializeField] private TextMeshProUGUI popupText;

        [Header("Настройки всплывающего текста")]
        [SerializeField] private float popupDuration = 2.0f;

        private float _popupTimer;
        private bool _popupActive;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // КРИТИЧНО: UI должен работать только у локального игрока.
            // Для других игроков в сети этот компонент просто отключается.
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            // АВТОПОИСК зависимостей в сцене (CasinoBank висит на сцене, а не на игроке)
            if (casinoBank == null)
                casinoBank = FindAnyObjectByType<CasinoBank>();

            if (vfxNotifier == null)
                vfxNotifier = FindAnyObjectByType<BankVfxNotifier>();

            if (casinoBank == null)
            {
                Debug.LogError("[BankUI] CasinoBank не найден в сцене! UI баланса не будет работать.");
                return;
            }

            // Подписываемся на события
            casinoBank.OnBalanceChanged += HandleBalanceChanged;

            UpdateBalanceDisplay(0, casinoBank.CurrentBalance); // Инициализация стартового баланса

            if (vfxNotifier != null)
            {
                vfxNotifier.OnTransactionVfxReceived += HandleTransactionVfx;
            }
            else
            {
                Debug.LogWarning("[BankUI] BankVfxNotifier не найден. Всплывающие уведомления работать не будут.");
            }
        }

        public override void OnNetworkDespawn()
        {
            // Обязательно отписываемся при уничтожении игрока (дисконнект и т.д.)
            if (IsOwner)
            {
                if (casinoBank != null)
                    casinoBank.OnBalanceChanged -= HandleBalanceChanged;

                if (vfxNotifier != null)
                    vfxNotifier.OnTransactionVfxReceived -= HandleTransactionVfx;
            }

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsOwner) return; // Защита на всякий случай

            // Логика затухания всплывающего текста
            if (_popupActive)
            {
                _popupTimer -= Time.deltaTime;
                if (popupText != null)
                {
                    float alpha = Mathf.Clamp01(_popupTimer / popupDuration);
                    Color color = popupText.color;
                    color.a = alpha;
                    popupText.color = color;

                    // Сдвигаем текст вверх
                    popupText.rectTransform.anchoredPosition += Vector2.up * Time.deltaTime * 30f;
                }

                if (_popupTimer <= 0f)
                {
                    _popupActive = false;
                    if (popupText != null) popupText.gameObject.SetActive(false);
                }
            }
        }

        private void HandleBalanceChanged(int oldBalance, int newBalance)
        {
            UpdateBalanceDisplay(oldBalance, newBalance);
        }

        private void UpdateBalanceDisplay(int oldBalance, int newBalance)
        {
            if (balanceText == null) return;

            balanceText.text = $"Касса: {newBalance}";

            if (newBalance > oldBalance) balanceText.color = Color.green;
            else if (newBalance < oldBalance) balanceText.color = Color.red;
            else balanceText.color = Color.white;
        }

        private void HandleTransactionVfx(TransactionVfxPacket packet)
        {
            ShowPopup(packet);
            // PlayTransactionSound(packet);
        }

        private void ShowPopup(TransactionVfxPacket packet)
        {
            if (popupText == null) return;

            popupText.gameObject.SetActive(true);
            popupText.rectTransform.anchoredPosition = Vector2.zero;

            string sign = packet.Type == TransactionType.Deposit ? "+" : "-";
            popupText.text = $"{sign}{packet.Amount}\n{packet.Reason}";
            popupText.color = packet.Type == TransactionType.Deposit ? Color.green : Color.red;

            _popupTimer = popupDuration;
            _popupActive = true;
        }
    }
}