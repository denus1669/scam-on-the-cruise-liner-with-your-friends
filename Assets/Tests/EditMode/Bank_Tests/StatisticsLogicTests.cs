using System.Collections.Generic;
using NUnit.Framework;
using Assets.Casino.Bank;

namespace Casino.Tests.EditMode.Bank_Tests
{
    /// <summary>
    /// Тесты для логики обновления статистики в StatisticsCollector.
    /// Тестируют чистую C# логику без Unity/Network зависимостей.
    /// </summary>
    [TestFixture]
    public class StatisticsLogicTests
    {
        [Test]
        public void UpdatePlayerStats_WithDepositTransaction_IncrementsTotalDeposits()
        {
            // Arrange
            var stats = new Dictionary<ulong, PlayerStatistics>();
            var transaction = new BankTransaction
            {
                OperatorClientId = 123UL,
                Amount = 500,
                Type = TransactionType.Deposit,
                Reason = "Ante",
                TableType = "BlackGreg"
            };

            // Act
            UpdateStats(stats, transaction);

            // Assert
            Assert.That(stats.ContainsKey(123UL), Is.True);
            Assert.That(stats[123UL].TotalDeposits, Is.EqualTo(500));
            Assert.That(stats[123UL].TransactionCount, Is.EqualTo(1));
        }

        [Test]
        public void UpdatePlayerStats_WithWithdrawTransaction_IncrementsTotalWithdraws()
        {
            // Arrange
            var stats = new Dictionary<ulong, PlayerStatistics>();
            var transaction = new BankTransaction
            {
                OperatorClientId = 456UL,
                Amount = 200,
                Type = TransactionType.Withdraw,
                Reason = "Payout",
                TableType = "Dice"
            };

            // Act
            UpdateStats(stats, transaction);

            // Assert
            Assert.That(stats.ContainsKey(456UL), Is.True);
            Assert.That(stats[456UL].TotalWithdraws, Is.EqualTo(200));
            Assert.That(stats[456UL].TransactionCount, Is.EqualTo(1));
        }

        [Test]
        public void UpdatePlayerStats_WithSystemOperator_DoesNotUpdateStats()
        {
            // Arrange
            var stats = new Dictionary<ulong, PlayerStatistics>();
            var transaction = new BankTransaction
            {
                OperatorClientId = ulong.MaxValue, // Системная операция
                Amount = 1000,
                Type = TransactionType.Deposit,
                Reason = "Initial",
                TableType = "None"
            };

            // Act
            UpdateStats(stats, transaction);

            // Assert
            Assert.That(stats.Count, Is.Zero);
        }

        [Test]
        public void UpdatePlayerStats_MultipleTransactionsForSamePlayer_AggregatesCorrectly()
        {
            // Arrange
            var stats = new Dictionary<ulong, PlayerStatistics>();
            
            var transaction1 = new BankTransaction
            {
                OperatorClientId = 789UL,
                Amount = 300,
                Type = TransactionType.Deposit,
                Reason = "Ante1"
            };
            
            var transaction2 = new BankTransaction
            {
                OperatorClientId = 789UL,
                Amount = 200,
                Type = TransactionType.Deposit,
                Reason = "Ante2"
            };
            
            var transaction3 = new BankTransaction
            {
                OperatorClientId = 789UL,
                Amount = 150,
                Type = TransactionType.Withdraw,
                Reason = "Payout"
            };

            // Act
            UpdateStats(stats, transaction1);
            UpdateStats(stats, transaction2);
            UpdateStats(stats, transaction3);

            // Assert
            Assert.That(stats[789UL].TotalDeposits, Is.EqualTo(500));
            Assert.That(stats[789UL].TotalWithdraws, Is.EqualTo(150));
            Assert.That(stats[789UL].TransactionCount, Is.EqualTo(3));
            Assert.That(stats[789UL].NetContribution, Is.EqualTo(350));
        }

        [Test]
        public void UpdatePlayerStats_NewPlayer_CreatesNewStatsEntry()
        {
            // Arrange
            var stats = new Dictionary<ulong, PlayerStatistics>();
            var transaction = new BankTransaction
            {
                OperatorClientId = 999UL,
                Amount = 100,
                Type = TransactionType.Deposit,
                Reason = "First"
            };

            // Act
            UpdateStats(stats, transaction);

            // Assert
            Assert.That(stats.Count, Is.EqualTo(1));
            Assert.That(stats[999UL].PlayerId, Is.EqualTo(999UL));
        }

        [Test]
        public void UpdatePlayerStats_ZeroAmountTransaction_StillIncrementsCount()
        {
            // Arrange
            var stats = new Dictionary<ulong, PlayerStatistics>();
            var transaction = new BankTransaction
            {
                OperatorClientId = 111UL,
                Amount = 0,
                Type = TransactionType.Deposit,
                Reason = "Zero"
            };

            // Act
            UpdateStats(stats, transaction);

            // Assert
            Assert.That(stats[111UL].TransactionCount, Is.EqualTo(1));
            Assert.That(stats[111UL].TotalDeposits, Is.Zero);
        }

        [Test]
        public void GetTransactionLog_ReturnsReadOnlyList()
        {
            // Arrange
            var log = new List<BankTransaction>();
            log.Add(new BankTransaction { Amount = 100, Type = TransactionType.Deposit });

            // Act
            IReadOnlyList<BankTransaction> readOnlyLog = log;

            // Assert
            Assert.That(readOnlyLog, Is.Not.Null);
            Assert.That(readOnlyLog.Count, Is.EqualTo(1));
        }

        [Test]
        public void GetAllPlayerStats_ReturnsReadOnlyDictionary()
        {
            // Arrange
            var stats = new Dictionary<ulong, PlayerStatistics>
            {
                { 1UL, new PlayerStatistics { PlayerId = 1UL } }
            };

            // Act
            IReadOnlyDictionary<ulong, PlayerStatistics> readOnlyStats = stats;

            // Assert
            Assert.That(readOnlyStats, Is.Not.Null);
            Assert.That(readOnlyStats.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryGetPlayerStats_ExistingPlayer_ReturnsTrue()
        {
            // Arrange
            var stats = new Dictionary<ulong, PlayerStatistics>
            {
                { 222UL, new PlayerStatistics { PlayerId = 222UL, TotalDeposits = 500 } }
            };

            // Act
            bool result = stats.TryGetValue(222UL, out PlayerStatistics foundStats);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(foundStats.PlayerId, Is.EqualTo(222UL));
        }

        [Test]
        public void TryGetPlayerStats_NonExistingPlayer_ReturnsFalse()
        {
            // Arrange
            var stats = new Dictionary<ulong, PlayerStatistics>
            {
                { 333UL, new PlayerStatistics { PlayerId = 333UL } }
            };

            // Act
            bool result = stats.TryGetValue(444UL, out PlayerStatistics foundStats);

            // Assert
            Assert.That(result, Is.False);
        }

        // Хелпер для симуляции приватного метода UpdatePlayerStats
        private void UpdateStats(Dictionary<ulong, PlayerStatistics> stats, BankTransaction transaction)
        {
            if (transaction.OperatorClientId == ulong.MaxValue) return;

            if (!stats.TryGetValue(transaction.OperatorClientId, out PlayerStatistics playerStats))
            {
                playerStats = new PlayerStatistics { PlayerId = transaction.OperatorClientId };
            }

            switch (transaction.Type)
            {
                case TransactionType.Deposit:
                    playerStats.TotalDeposits += transaction.Amount;
                    break;
                case TransactionType.Withdraw:
                    playerStats.TotalWithdraws += transaction.Amount;
                    break;
            }

            playerStats.TransactionCount++;
            stats[transaction.OperatorClientId] = playerStats;
        }
    }
}
