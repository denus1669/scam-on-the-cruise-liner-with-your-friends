
    /// <summary>
    /// Тип изменения баланса кассы.
    /// </summary>
    public enum TransactionType
    {
        Deposit,  // Поступление в кассу
        Withdraw  // Списание из кассы
    }

    /// <summary>
    /// Структура, описывающая факт изменения баланса.
    /// Используется для серверных событий и будущего сборщика статистики.
    /// Значимый тип (struct) — не создаёт мусор для GC.
    /// </summary>
    public struct BankTransaction
    {
        /// <summary>ID сотрудника, инициировавшего операцию (ulong.MaxValue для системных операций).</summary>
        public ulong OperatorClientId;

        /// <summary>Сумма операции (всегда положительное число).</summary>
        public int Amount;

        /// <summary>Тип операции.</summary>
        public TransactionType Type;

        /// <summary>Причина операции ("Ante", "CheatCaught", "SlapIdlePenalty" и т.д.).</summary>
        public string Reason;

        /// <summary>Тип стола, где произошла операция ("BlackGreg", "Dice" и т.д.).</summary>
        public string TableType;

        /// <summary>Серверное время операции для сортировки в статистике.</summary>
        public double Timestamp;
    }
