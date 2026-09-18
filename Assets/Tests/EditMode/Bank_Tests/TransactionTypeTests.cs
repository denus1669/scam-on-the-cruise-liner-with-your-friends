using NUnit.Framework;
using Assets.Casino.Bank;

namespace Casino.Tests.EditMode.Bank_Tests
{
    /// <summary>
    /// Тесты для enum TransactionType.
    /// Проверяют корректность значений перечисления.
    /// </summary>
    [TestFixture]
    public class TransactionTypeTests
    {
        [Test]
        public void Deposit_HasValue_Zero()
        {
            // Assert
            Assert.That((int)TransactionType.Deposit, Is.EqualTo(0));
        }

        [Test]
        public void Withdraw_HasValue_One()
        {
            // Assert
            Assert.That((int)TransactionType.Withdraw, Is.EqualTo(1));
        }

        [Test]
        public void Deposit_CanBeCastToByte()
        {
            // Arrange & Act
            byte depositByte = (byte)TransactionType.Deposit;

            // Assert
            Assert.That(depositByte, Is.EqualTo(0));
        }

        [Test]
        public void Withdraw_CanBeCastToByte()
        {
            // Arrange & Act
            byte withdrawByte = (byte)TransactionType.Withdraw;

            // Assert
            Assert.That(withdrawByte, Is.EqualTo(1));
        }

        [Test]
        public void ByteZero_CanBeCastToDeposit()
        {
            // Arrange & Act
            var type = (TransactionType)0;

            // Assert
            Assert.That(type, Is.EqualTo(TransactionType.Deposit));
        }

        [Test]
        public void ByteOne_CanBeCastToWithdraw()
        {
            // Arrange & Act
            var type = (TransactionType)1;

            // Assert
            Assert.That(type, Is.EqualTo(TransactionType.Withdraw));
        }
    }
}
