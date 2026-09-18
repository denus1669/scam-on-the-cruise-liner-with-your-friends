using NUnit.Framework;
using Assets.Casino.Attention;

namespace Casino.Tests.EditMode.Attention_Tests
{
    /// <summary>
    /// Тесты для интерфейса IAttentionTarget.
    /// Проверяют сигнатуры методов и контракты интерфейса.
    /// </summary>
    [TestFixture]
    public class IAttentionTargetTests
    {
        // Моковая реализация интерфейса для тестирования
        private class MockAttentionTarget : IAttentionTarget
        {
            public ulong LastEnterClientId { get; private set; }
            public ulong LastExitClientId { get; private set; }
            public int EnterCallCount { get; private set; }
            public int ExitCallCount { get; private set; }

            public void OnAttentionEnter(ulong watcherClientId)
            {
                LastEnterClientId = watcherClientId;
                EnterCallCount++;
            }

            public void OnAttentionExit(ulong watcherClientId)
            {
                LastExitClientId = watcherClientId;
                ExitCallCount++;
            }
        }

        [Test]
        public void OnAttentionEnter_WithValidClientId_StoresClientId()
        {
            // Arrange
            var target = new MockAttentionTarget();
            ulong testClientId = 12345UL;

            // Act
            target.OnAttentionEnter(testClientId);

            // Assert
            Assert.That(target.LastEnterClientId, Is.EqualTo(testClientId));
            Assert.That(target.EnterCallCount, Is.EqualTo(1));
        }

        [Test]
        public void OnAttentionExit_WithValidClientId_StoresClientId()
        {
            // Arrange
            var target = new MockAttentionTarget();
            ulong testClientId = 67890UL;

            // Act
            target.OnAttentionExit(testClientId);

            // Assert
            Assert.That(target.LastExitClientId, Is.EqualTo(testClientId));
            Assert.That(target.ExitCallCount, Is.EqualTo(1));
        }

        [Test]
        public void OnAttentionEnter_MultipleCalls_IncrementsCount()
        {
            // Arrange
            var target = new MockAttentionTarget();

            // Act
            target.OnAttentionEnter(1UL);
            target.OnAttentionEnter(2UL);
            target.OnAttentionEnter(3UL);

            // Assert
            Assert.That(target.EnterCallCount, Is.EqualTo(3));
            Assert.That(target.LastEnterClientId, Is.EqualTo(3UL));
        }

        [Test]
        public void OnAttentionExit_MultipleCalls_IncrementsCount()
        {
            // Arrange
            var target = new MockAttentionTarget();

            // Act
            target.OnAttentionExit(1UL);
            target.OnAttentionExit(2UL);

            // Assert
            Assert.That(target.ExitCallCount, Is.EqualTo(2));
            Assert.That(target.LastExitClientId, Is.EqualTo(2UL));
        }

        [Test]
        public void OnAttentionEnter_WithZeroClientId_AcceptsZeroId()
        {
            // Arrange
            var target = new MockAttentionTarget();

            // Act
            target.OnAttentionEnter(0UL);

            // Assert
            Assert.That(target.LastEnterClientId, Is.Zero);
            Assert.That(target.EnterCallCount, Is.EqualTo(1));
        }

        [Test]
        public void OnAttentionExit_WithMaxClientId_AcceptsMaxId()
        {
            // Arrange
            var target = new MockAttentionTarget();

            // Act
            target.OnAttentionExit(ulong.MaxValue);

            // Assert
            Assert.That(target.LastExitClientId, Is.EqualTo(ulong.MaxValue));
            Assert.That(target.ExitCallCount, Is.EqualTo(1));
        }

        [Test]
        public void OnAttentionEnter_AndExit_Sequentially_WorksCorrectly()
        {
            // Arrange
            var target = new MockAttentionTarget();
            ulong clientId = 999UL;

            // Act
            target.OnAttentionEnter(clientId);
            target.OnAttentionExit(clientId);

            // Assert
            Assert.That(target.EnterCallCount, Is.EqualTo(1));
            Assert.That(target.ExitCallCount, Is.EqualTo(1));
            Assert.That(target.LastEnterClientId, Is.EqualTo(clientId));
            Assert.That(target.LastExitClientId, Is.EqualTo(clientId));
        }

        [Test]
        public void OnAttentionEnter_WithDifferentClients_TracksAll()
        {
            // Arrange
            var target = new MockAttentionTarget();

            // Act
            target.OnAttentionEnter(100UL);
            target.OnAttentionEnter(200UL);
            target.OnAttentionEnter(300UL);

            // Assert
            Assert.That(target.EnterCallCount, Is.EqualTo(3));
            Assert.That(target.LastEnterClientId, Is.EqualTo(300UL));
        }
    }
}
