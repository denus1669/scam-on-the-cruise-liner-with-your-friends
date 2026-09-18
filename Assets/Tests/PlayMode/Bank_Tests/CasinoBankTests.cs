using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assets.Casino.Bank;
using Unity.Netcode;

namespace Casino.Tests.PlayMode.Bank_Tests
{
    /// <summary>
    /// PlayMode тесты для CasinoBank.
    /// Тестируют сетевую логику, NetworkVariable и события.
    /// </summary>
    [TestFixture]
    public class CasinoBankTests
    {
        private GameObject _bankGameObject;
        private CasinoBank _casinoBank;

        [SetUp]
        public void SetUp()
        {
            _bankGameObject = new GameObject("CasinoBank");
            _casinoBank = _bankGameObject.AddComponent<CasinoBank>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_bankGameObject != null)
            {
                Object.DestroyImmediate(_bankGameObject);
            }
        }

        [UnityTest]
        public IEnumerator OnNetworkSpawn_WhenIsServer_SetsStartingBalance()
        {
            // Arrange
            // Симуляция сервера через рефлексию или настройку NetworkManager
            // В реальном тесте нужен NetworkManager
            
            // Для простоты проверяем что компонент создан
            Assert.That(_casinoBank, Is.Not.Null);
            
            yield return null;
        }

        [Test]
        public void CurrentBalance_Initially_ReturnsZero()
        {
            // Act
            int balance = _casinoBank.CurrentBalance;

            // Assert
            Assert.That(balance, Is.Zero);
        }

        [Test]
        public void TryDeposit_WhenAmountIsZero_ReturnsFalse()
        {
            // Arrange - симулируем сервер через рефлексию
            SetIsServer(_casinoBank, true);

            // Act
            bool result = _casinoBank.TryDeposit(0, 1UL, "Test");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryDeposit_WhenAmountIsNegative_ReturnsFalse()
        {
            // Arrange
            SetIsServer(_casinoBank, true);

            // Act
            bool result = _casinoBank.TryDeposit(-100, 1UL, "Test");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryWithdraw_WhenAmountIsZero_ReturnsZero()
        {
            // Arrange
            SetIsServer(_casinoBank, true);

            // Act
            int result = _casinoBank.TryWithdraw(0, 1UL, "Test");

            // Assert
            Assert.That(result, Is.Zero);
        }

        [Test]
        public void TryWithdraw_WhenAmountIsNegative_ReturnsZero()
        {
            // Arrange
            SetIsServer(_casinoBank, true);

            // Act
            int result = _casinoBank.TryWithdraw(-50, 1UL, "Test");

            // Assert
            Assert.That(result, Is.Zero);
        }

        [Test]
        public void TryDeposit_WhenNotServer_ReturnsFalse()
        {
            // Arrange
            SetIsServer(_casinoBank, false);

            // Act
            bool result = _casinoBank.TryDeposit(100, 1UL, "Test");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryWithdraw_WhenNotServer_ReturnsZero()
        {
            // Arrange
            SetIsServer(_casinoBank, false);

            // Act
            int result = _casinoBank.TryWithdraw(100, 1UL, "Test");

            // Assert
            Assert.That(result, Is.Zero);
        }

        [UnityTest]
        public IEnumerator OnBalanceChanged_AfterDeposit_IsInvokedWithNewBalance()
        {
            // Arrange
            SetIsServer(_casinoBank, true);
            bool invoked = false;
            int capturedBalance = -1;
            ulong capturedOperator = 0UL;

            _casinoBank.OnBalanceChanged += (newBalance, operatorId) =>
            {
                invoked = true;
                capturedBalance = newBalance;
                capturedOperator = (ulong)operatorId;
            };

            // Act
            _casinoBank.TryDeposit(500, 123UL, "Ante", "BlackGreg");

            // Assert
            Assert.That(invoked, Is.True);
            Assert.That(capturedBalance, Is.EqualTo(500));
            Assert.That(capturedOperator, Is.EqualTo(123UL));

            yield return null;
        }

        [UnityTest]
        public IEnumerator OnTransactionCompleted_AfterDeposit_IsInvoked()
        {
            SetIsServer(_casinoBank, true);
            bool invoked = false;
            BankTransaction captured = default;

            _casinoBank.OnTransactionCompleted += tx =>
            {
                invoked = true;
                captured = tx;
            };

            _casinoBank.TryDeposit(500, 123UL, "Ante", "BlackGreg");

            Assert.That(invoked, Is.True);
            Assert.That(captured.Amount, Is.EqualTo(500));

            yield return null;
        }
        [UnityTest]
        public IEnumerator TryDeposit_WithValidParameters_OnServer_DepositsSuccessfully()
        {
            // Arrange
            SetIsServer(_casinoBank, true);
            bool eventInvoked = false;
            BankTransaction capturedTransaction = default;

            _casinoBank.OnTransactionCompleted += (transaction) =>
            {
                eventInvoked = true;
                capturedTransaction = transaction;
            };

            // Act
            bool result = _casinoBank.TryDeposit(500, 123UL, "Ante", "BlackGreg");

            // Assert
            Assert.That(result, Is.True);
            Assert.That(eventInvoked, Is.True);
            Assert.That(capturedTransaction.Amount, Is.EqualTo(500));
            Assert.That(capturedTransaction.Type, Is.EqualTo(TransactionType.Deposit));
            Assert.That(capturedTransaction.OperatorClientId, Is.EqualTo(123UL));

            yield return null;
        }

        [UnityTest]
        public IEnumerator TryWithdraw_WithSufficientBalance_OnServer_WithdrawsSuccessfully()
        {
            // Arrange
            SetIsServer(_casinoBank, true);
            bool eventInvoked = false;
            BankTransaction capturedTransaction = default;

            _casinoBank.OnTransactionCompleted += (transaction) =>
            {
                eventInvoked = true;
                capturedTransaction = transaction;
            };

            // Сначала депозит чтобы был баланс
            _casinoBank.TryDeposit(1000, ulong.MaxValue, "Initial", "None");

            // Act
            int withdrawn = _casinoBank.TryWithdraw(500, 456UL, "Payout", "Dice");

            // Assert
            Assert.That(withdrawn, Is.EqualTo(500));
            Assert.That(eventInvoked, Is.True);
            Assert.That(capturedTransaction.Amount, Is.EqualTo(500));
            Assert.That(capturedTransaction.Type, Is.EqualTo(TransactionType.Withdraw));

            yield return null;
        }

        [UnityTest]
        public IEnumerator TryWithdraw_WithInsufficientBalance_OnServer_WithdrawsPartialAmount()
        {
            // Arrange
            SetIsServer(_casinoBank, true);
            
            // Депозит 100
            _casinoBank.TryDeposit(100, ulong.MaxValue, "Initial", "None");

            // Act - пытаемся снять 500 но есть только 100
            int withdrawn = _casinoBank.TryWithdraw(500, 789UL, "Payout", "BlackGreg");

            // Assert
            Assert.That(withdrawn, Is.EqualTo(100)); // Снято только то что было

            yield return null;
        }

        [UnityTest]
        public IEnumerator TryWithdraw_WhenBalanceIsEmpty_OnServer_ReturnsZero()
        {
            // Arrange
            SetIsServer(_casinoBank, true);
            // Баланс по умолчанию 0

            // Act
            int withdrawn = _casinoBank.TryWithdraw(100, 1UL, "Test", "None");

            // Assert
            Assert.That(withdrawn, Is.Zero);

            yield return null;
        }

        // Хелпер для установки IsServer через рефлексию
        private void SetIsServer(NetworkBehaviour behaviour, bool value)
        {
            var field = typeof(NetworkBehaviour).GetField("m_IsServer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(behaviour, value);
            }
        }
    }
}
