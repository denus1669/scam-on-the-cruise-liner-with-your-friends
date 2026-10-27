using System;
using Assets.Casino.Games;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Casino.Bank
{
    /// <summary>
    /// Хранилище и оператор кассы казино.
    /// Отвечает ТОЛЬКО за баланс и валидацию операций.
    /// Не занимается статистикой, VFX или UI — они подписываются на события.
    /// </summary>
    public class CasinoBank : NetworkBehaviour, ICasinoBank
    {
        [Header("Начальные настройки")]
        [SerializeField] private int startingBalance = 1000;

        private readonly NetworkVariable<int> _balance = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private ICasinoBank _casinoBankImplementation;

        public int CurrentBalance => _balance.Value;

        public event Action<int, int> OnBalanceChanged;
        public event Action<BankTransaction> OnTransactionCompleted;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                _balance.Value = startingBalance;
            }

            _balance.OnValueChanged += HandleBalanceChanged;
            OnBalanceChanged?.Invoke(0, _balance.Value);

        }

        public override void OnNetworkDespawn()
        {
            _balance.OnValueChanged -= HandleBalanceChanged;
            base.OnNetworkDespawn();
        }

        private void HandleBalanceChanged(int previousValue, int newValue)
        {
            OnBalanceChanged?.Invoke(previousValue, newValue);
        }

        public bool TryDeposit(int amount, ulong operatorId, string reason, GameType tableType = GameType.None)
        {
            if (!IsServer) { Debug.LogWarning("[CasinoBank] TryDeposit can only be called on server."); return false; }
            if (amount <= 0) { Debug.LogWarning($"[CasinoBank] Неверная сумма пополнения: {amount}"); return false; }

            _balance.Value += amount;
            RecordTransaction(operatorId, amount, TransactionType.Deposit, reason, tableType);
            return true;
        }

        public int TryWithdraw(int amount, ulong operatorId, string reason, GameType tableType = GameType.None)
        {
            if (!IsServer) { Debug.LogWarning("[CasinoBank] TryWithdraw can only be called on server."); return 0; }
            if (amount <= 0) { Debug.LogWarning($"[CasinoBank] Неверная сумма списания: {amount}"); return 0; }

            int currentBalance = _balance.Value;
            int actualWithdrawn = Math.Min(amount, currentBalance);
            if (actualWithdrawn <= 0) { Debug.LogWarning("[CasinoBank] Касса пуста, списание невозможно."); return 0; }

            _balance.Value = currentBalance - actualWithdrawn;
            RecordTransaction(operatorId, actualWithdrawn, TransactionType.Withdraw, reason, tableType);
            return actualWithdrawn;
        }

        private void RecordTransaction(ulong operatorId, int amount, TransactionType type, string reason, GameType tableType)
        {
            var transaction = new BankTransaction
            {
                OperatorClientId = operatorId,
                Amount = amount,
                Type = type,
                Reason = reason ?? "Unknown",
                TableType = tableType,           // ← без ?? "None"
                Timestamp = NetworkManager.Singleton?.ServerTime.FixedDeltaTimeAsDouble ?? 0.0
            };

            OnTransactionCompleted?.Invoke(transaction);
        }
    }
}
