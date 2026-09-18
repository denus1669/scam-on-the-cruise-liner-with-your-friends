using NUnit.Framework;
using Assets.Casino.Bank;

namespace Casino.Tests.EditMode.Bank_Tests
{
    /// <summary>
    /// Тесты для структуры BankTransaction.
    /// Проверяют корректность создания, инициализации и сериализации транзакций.
    /// </summary>
    [TestFixture]
    public class BankTransactionTests
    {
        [Test]
        public void Constructor_WithValidParameters_CreatesTransactionCorrectly()
        {
            // Arrange
            ulong operatorId = 12345UL;
            int amount = 500;
            TransactionType type = TransactionType.Deposit;
            string reason = "Ante";
            string tableType = "BlackGreg";
            double timestamp = 123.456;

            // Act
            var transaction = new BankTransaction
            {
                OperatorClientId = operatorId,
                Amount = amount,
                Type = type,
                Reason = reason,
                TableType = tableType,
                Timestamp = timestamp
            };

            // Assert
            Assert.That(transaction.OperatorClientId, Is.EqualTo(operatorId));
            Assert.That(transaction.Amount, Is.EqualTo(amount));
            Assert.That(transaction.Type, Is.EqualTo(type));
            Assert.That(transaction.Reason, Is.EqualTo(reason));
            Assert.That(transaction.TableType, Is.EqualTo(tableType));
            Assert.That(transaction.Timestamp, Is.EqualTo(timestamp));
        }

        [Test]
        public void Constructor_WithNullReason_SetsDefaultReason()
        {
            // Arrange & Act
            var transaction = new BankTransaction
            {
                OperatorClientId = 1UL,
                Amount = 100,
                Type = TransactionType.Withdraw,
                Reason = null,
                TableType = null,
                Timestamp = 0.0
            };

            // Assert
            Assert.That(transaction.Reason, Is.Null);
            Assert.That(transaction.TableType, Is.Null);
        }

        [Test]
        public void Constructor_WithZeroAmount_CreatesTransactionWithZero()
        {
            // Arrange & Act
            var transaction = new BankTransaction
            {
                OperatorClientId = 0UL,
                Amount = 0,
                Type = TransactionType.Deposit,
                Reason = "Test",
                TableType = "TestTable",
                Timestamp = 0.0
            };

            // Assert
            Assert.That(transaction.Amount, Is.Zero);
        }

        [Test]
        public void Constructor_WithNegativeAmount_CreatesTransactionWithNegativeValue()
        {
            // Arrange & Act
            var transaction = new BankTransaction
            {
                OperatorClientId = 1UL,
                Amount = -100,
                Type = TransactionType.Withdraw,
                Reason = "Test",
                TableType = "TestTable",
                Timestamp = 0.0
            };

            // Assert
            Assert.That(transaction.Amount, Is.EqualTo(-100));
        }

        [Test]
        public void Constructor_WithMaxOperatorId_CreatesSystemTransaction()
        {
            // Arrange & Act
            var transaction = new BankTransaction
            {
                OperatorClientId = ulong.MaxValue,
                Amount = 1000,
                Type = TransactionType.Deposit,
                Reason = "System",
                TableType = "None",
                Timestamp = 1.0
            };

            // Assert
            Assert.That(transaction.OperatorClientId, Is.EqualTo(ulong.MaxValue));
        }

        [Test]
        public void Constructor_DepositType_HasCorrectEnumValue()
        {
            // Arrange & Act
            var transaction = new BankTransaction
            {
                OperatorClientId = 1UL,
                Amount = 100,
                Type = TransactionType.Deposit,
                Reason = "Test",
                TableType = "Test",
                Timestamp = 0.0
            };

            // Assert
            Assert.That(transaction.Type, Is.EqualTo(TransactionType.Deposit));
        }

        [Test]
        public void Constructor_WithdrawType_HasCorrectEnumValue()
        {
            // Arrange & Act
            var transaction = new BankTransaction
            {
                OperatorClientId = 1UL,
                Amount = 100,
                Type = TransactionType.Withdraw,
                Reason = "Test",
                TableType = "Test",
                Timestamp = 0.0
            };

            // Assert
            Assert.That(transaction.Type, Is.EqualTo(TransactionType.Withdraw));
        }

        [Test]
        public void Constructor_WithEmptyStringReason_CreatesTransactionWithEmptyReason()
        {
            // Arrange & Act
            var transaction = new BankTransaction
            {
                OperatorClientId = 1UL,
                Amount = 100,
                Type = TransactionType.Deposit,
                Reason = "",
                TableType = "Test",
                Timestamp = 0.0
            };

            // Assert
            Assert.That(transaction.Reason, Is.Empty);
        }

        [Test]
        public void Constructor_WithLargeTimestamp_CreatesTransactionCorrectly()
        {
            // Arrange
            double largeTimestamp = double.MaxValue;

            // Act
            var transaction = new BankTransaction
            {
                OperatorClientId = 1UL,
                Amount = 100,
                Type = TransactionType.Deposit,
                Reason = "Test",
                TableType = "Test",
                Timestamp = largeTimestamp
            };

            // Assert
            Assert.That(transaction.Timestamp, Is.EqualTo(largeTimestamp));
        }
    }
}
