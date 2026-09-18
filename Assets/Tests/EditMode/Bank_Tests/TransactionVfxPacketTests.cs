using NUnit.Framework;
using Assets.Casino.Bank;
using Unity.Collections;   
using Unity.Netcode;        


namespace Casino.Tests.EditMode.Bank_Tests
{
    /// <summary>
    /// Тесты для структуры TransactionVfxPacket.
    /// Проверяют корректность сетевой сериализации и данных.
    /// </summary>
    [TestFixture]
    public class TransactionVfxPacketTests
    {
        [Test]
        public void Constructor_WithValidParameters_CreatesPacketCorrectly()
        {
            // Arrange
            ulong operatorId = 12345UL;
            int amount = 500;
            TransactionType type = TransactionType.Deposit;
            string reason = "Ante";
            string tableType = "BlackGreg";

            // Act
            var packet = new TransactionVfxPacket
            {
                OperatorClientId = operatorId,
                Amount = amount,
                Type = type,
                Reason = reason,
                TableType = tableType
            };

            // Assert
            Assert.That(packet.OperatorClientId, Is.EqualTo(operatorId));
            Assert.That(packet.Amount, Is.EqualTo(amount));
            Assert.That(packet.Type, Is.EqualTo(type));
            Assert.That(packet.Reason, Is.EqualTo(reason));
            Assert.That(packet.TableType, Is.EqualTo(tableType));
        }

        [Test]
        public void Constructor_WithNullReason_SetsNullReason()
        {
            // Arrange & Act
            var packet = new TransactionVfxPacket
            {
                OperatorClientId = 1UL,
                Amount = 100,
                Type = TransactionType.Withdraw,
                Reason = null,
                TableType = null
            };

            // Assert
            Assert.That(packet.Reason, Is.Null);
            Assert.That(packet.TableType, Is.Null);
        }

        [Test]
        public void Constructor_WithZeroAmount_CreatesPacketWithZero()
        {
            // Arrange & Act
            var packet = new TransactionVfxPacket
            {
                OperatorClientId = 0UL,
                Amount = 0,
                Type = TransactionType.Deposit,
                Reason = "Test",
                TableType = "TestTable"
            };

            // Assert
            Assert.That(packet.Amount, Is.Zero);
        }

        [Test]
        public void Constructor_WithNegativeAmount_CreatesPacketWithNegativeValue()
        {
            // Arrange & Act
            var packet = new TransactionVfxPacket
            {
                OperatorClientId = 1UL,
                Amount = -100,
                Type = TransactionType.Withdraw,
                Reason = "Test",
                TableType = "TestTable"
            };

            // Assert
            Assert.That(packet.Amount, Is.EqualTo(-100));
        }

        [Test]
        public void Constructor_WithMaxOperatorId_CreatesSystemPacket()
        {
            // Arrange & Act
            var packet = new TransactionVfxPacket
            {
                OperatorClientId = ulong.MaxValue,
                Amount = 1000,
                Type = TransactionType.Deposit,
                Reason = "System",
                TableType = "None"
            };

            // Assert
            Assert.That(packet.OperatorClientId, Is.EqualTo(ulong.MaxValue));
        }

        [Test]
        public void Constructor_DepositType_HasCorrectEnumValue()
        {
            // Arrange & Act
            var packet = new TransactionVfxPacket
            {
                OperatorClientId = 1UL,
                Amount = 100,
                Type = TransactionType.Deposit,
                Reason = "Test",
                TableType = "Test"
            };

            // Assert
            Assert.That(packet.Type, Is.EqualTo(TransactionType.Deposit));
        }

        [Test]
        public void Constructor_WithdrawType_HasCorrectEnumValue()
        {
            // Arrange & Act
            var packet = new TransactionVfxPacket
            {
                OperatorClientId = 1UL,
                Amount = 100,
                Type = TransactionType.Withdraw,
                Reason = "Test",
                TableType = "Test"
            };

            // Assert
            Assert.That(packet.Type, Is.EqualTo(TransactionType.Withdraw));
        }

        [Test]
        public void NetworkSerialize_Deserialize_RoundTripMaintainsData()
        {
            var original = new TransactionVfxPacket
            {
                OperatorClientId = 98765UL,
                Amount = 250,
                Type = TransactionType.Deposit,
                Reason = "WinBonus",
                TableType = "Slots"
            };

            using var writer = new FastBufferWriter(1024, Allocator.Temp);
            writer.WriteValueSafe(original.OperatorClientId);
            writer.WriteValueSafe(original.Amount);
            writer.WriteValueSafe(original.Type);
            writer.WriteValueSafe(original.Reason);
            writer.WriteValueSafe(original.TableType);

            using var reader = new FastBufferReader(writer, Allocator.Temp);
            reader.ReadValueSafe(out ulong opId);
            reader.ReadValueSafe(out int amount);
            reader.ReadValueSafe(out TransactionType type);
            reader.ReadValueSafe(out string reason);
            reader.ReadValueSafe(out string tableType);

            Assert.That(opId, Is.EqualTo(original.OperatorClientId));
            Assert.That(amount, Is.EqualTo(original.Amount));
            Assert.That(type, Is.EqualTo(original.Type));
            Assert.That(reason, Is.EqualTo(original.Reason));
            Assert.That(tableType, Is.EqualTo(original.TableType));
        }        
    }
}
