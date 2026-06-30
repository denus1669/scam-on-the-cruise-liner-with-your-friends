using System;


    /// <summary>
    /// Контракт кассы казино.
    /// Позволяет другим системам (столам, контроллерам мухлежа, шлепков) взаимодействовать
    /// с балансом, не зная конкретной реализации.
    /// </summary>
    public interface ICasinoBank
    {
        /// <summary>Текущий баланс кассы.</summary>
        int CurrentBalance { get; }

        /// <summary>
        /// Срабатывает при любом изменении баланса (для UI).
        /// Параметры: (oldBalance, newBalance).
        /// </summary>
        event Action<int, int> OnBalanceChanged;

        /// <summary>
        /// Срабатывает при успешной транзакции (для статистики и VFX).
        /// </summary>
        event Action<BankTransaction> OnTransactionCompleted;

        /// <summary>
        /// Пополнить кассу.
        /// </summary>
        /// <param name="amount">Сумма (должна быть &gt; 0).</param>
        /// <param name="operatorId">ID сотрудника или ulong.MaxValue для системы.</param>
        /// <param name="reason">Причина операции.</param>
        /// <param name="tableType">Тип стола (опционально, может быть null).</param>
        /// <returns>true, если операция успешна.</returns>
        bool TryDeposit(int amount, ulong operatorId, string reason, string tableType = null);

        /// <summary>
        /// Списать из кассы. Если средств недостаточно — списывает столько, сколько есть (до 0).
        /// </summary>
        /// <param name="amount">Запрашиваемая сумма (должна быть &gt; 0).</param>
        /// <param name="operatorId">ID сотрудника или ulong.MaxValue для системы.</param>
        /// <param name="reason">Причина операции.</param>
        /// <param name="tableType">Тип стола (опционально, может быть null).</param>
        /// <returns>Фактически списанная сумма (может быть меньше запрошенной).</returns>
        int TryWithdraw(int amount, ulong operatorId, string reason, string tableType = null);
    }
