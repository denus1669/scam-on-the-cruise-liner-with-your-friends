using NUnit.Framework;
using Assets.Casino.Bank;

namespace Casino.Tests.EditMode.Bank_Tests
{
    /// <summary>
    /// Тесты для структуры PlayerStatistics.
    /// Проверяют корректность создания, инициализации и вычислений статистики игрока.
    /// </summary>
    [TestFixture]
    public class PlayerStatisticsTests
    {
        [Test]
        public void Constructor_DefaultValues_InitializesCorrectly()
        {
            // Arrange & Act
            var stats = new PlayerStatistics();

            // Assert
            Assert.That(stats.PlayerId, Is.Zero);
            Assert.That(stats.TotalDeposits, Is.Zero);
            Assert.That(stats.TotalWithdraws, Is.Zero);
            Assert.That(stats.TransactionCount, Is.Zero);
        }

        [Test]
        public void Constructor_WithValidParameters_CreatesStatisticsCorrectly()
        {
            // Arrange
            ulong playerId = 12345UL;
            int totalDeposits = 5000;
            int totalWithdraws = 2000;
            int transactionCount = 15;

            // Act
            var stats = new PlayerStatistics
            {
                PlayerId = playerId,
                TotalDeposits = totalDeposits,
                TotalWithdraws = totalWithdraws,
                TransactionCount = transactionCount
            };

            // Assert
            Assert.That(stats.PlayerId, Is.EqualTo(playerId));
            Assert.That(stats.TotalDeposits, Is.EqualTo(totalDeposits));
            Assert.That(stats.TotalWithdraws, Is.EqualTo(totalWithdraws));
            Assert.That(stats.TransactionCount, Is.EqualTo(transactionCount));
        }

        [Test]
        public void NetContribution_WhenDepositsGreaterThanWithdraws_ReturnsPositiveValue()
        {
            // Arrange
            var stats = new PlayerStatistics
            {
                PlayerId = 1UL,
                TotalDeposits = 5000,
                TotalWithdraws = 2000,
                TransactionCount = 10
            };

            // Act
            int netContribution = stats.NetContribution;

            // Assert
            Assert.That(netContribution, Is.EqualTo(3000));
        }

        [Test]
        public void NetContribution_WhenWithdrawsGreaterThanDeposits_ReturnsNegativeValue()
        {
            // Arrange
            var stats = new PlayerStatistics
            {
                PlayerId = 1UL,
                TotalDeposits = 1000,
                TotalWithdraws = 3000,
                TransactionCount = 10
            };

            // Act
            int netContribution = stats.NetContribution;

            // Assert
            Assert.That(netContribution, Is.EqualTo(-2000));
        }

        [Test]
        public void NetContribution_WhenDepositsEqualsWithdraws_ReturnsZero()
        {
            // Arrange
            var stats = new PlayerStatistics
            {
                PlayerId = 1UL,
                TotalDeposits = 2500,
                TotalWithdraws = 2500,
                TransactionCount = 8
            };

            // Act
            int netContribution = stats.NetContribution;

            // Assert
            Assert.That(netContribution, Is.Zero);
        }

        [Test]
        public void NetContribution_WhenNoTransactions_ReturnsZero()
        {
            // Arrange
            var stats = new PlayerStatistics
            {
                PlayerId = 1UL,
                TotalDeposits = 0,
                TotalWithdraws = 0,
                TransactionCount = 0
            };

            // Act
            int netContribution = stats.NetContribution;

            // Assert
            Assert.That(netContribution, Is.Zero);
        }

        [Test]
        public void NetContribution_WhenOnlyDeposits_ReturnsDepositsAmount()
        {
            // Arrange
            var stats = new PlayerStatistics
            {
                PlayerId = 1UL,
                TotalDeposits = 7500,
                TotalWithdraws = 0,
                TransactionCount = 5
            };

            // Act
            int netContribution = stats.NetContribution;

            // Assert
            Assert.That(netContribution, Is.EqualTo(7500));
        }

        [Test]
        public void NetContribution_WhenOnlyWithdraws_ReturnsNegativeWithdrawsAmount()
        {
            // Arrange
            var stats = new PlayerStatistics
            {
                PlayerId = 1UL,
                TotalDeposits = 0,
                TotalWithdraws = 4000,
                TransactionCount = 3
            };

            // Act
            int netContribution = stats.NetContribution;

            // Assert
            Assert.That(netContribution, Is.EqualTo(-4000));
        }

        [Test]
        public void Constructor_WithMaxPlayerId_CreatesStatisticsCorrectly()
        {
            // Arrange & Act
            var stats = new PlayerStatistics
            {
                PlayerId = ulong.MaxValue,
                TotalDeposits = 1000,
                TotalWithdraws = 500,
                TransactionCount = 5
            };

            // Assert
            Assert.That(stats.PlayerId, Is.EqualTo(ulong.MaxValue));
        }

        [Test]
        public void Constructor_WithLargeTransactionCount_CreatesStatisticsCorrectly()
        {
            // Arrange & Act
            var stats = new PlayerStatistics
            {
                PlayerId = 1UL,
                TotalDeposits = int.MaxValue / 2,
                TotalWithdraws = int.MaxValue / 4,
                TransactionCount = int.MaxValue
            };

            // Assert
            Assert.That(stats.TransactionCount, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void NetContribution_LargeValues_DoesNotOverflow()
        {
            // Arrange
            var stats = new PlayerStatistics
            {
                PlayerId = 1UL,
                TotalDeposits = 1_000_000_000,
                TotalWithdraws = 500_000_000,
                TransactionCount = 1000
            };

            // Act
            int netContribution = stats.NetContribution;

            // Assert
            Assert.That(netContribution, Is.EqualTo(500_000_000));
        }
    }
}
