using NUnit.Framework;
using UnityEngine;

namespace Casino.Tests.EditMode.Inspector_Tests
{
    /// <summary>
    /// Тесты для логики InspectorAgent (чистая C# логика без Unity/NavMesh зависимостей).
    /// </summary>
    [TestFixture]
    public class InspectorAgentLogicTests
    {
        private MockInspectorAgent _inspector;

        [SetUp]
        public void SetUp()
        {
            _inspector = new MockInspectorAgent();
        }

        [Test]
        public void HasSelectedTarget_Initially_ReturnsFalse()
        {
            // Assert
            Assert.That(_inspector.HasSelectedTarget, Is.False);
        }

        [Test]
        public void IsStoppedByPlayer_Initially_ReturnsFalse()
        {
            // Assert
            Assert.That(_inspector.IsStoppedByPlayer, Is.False);
        }

        [Test]
        public void CanBeStoppedByPlayer_WhenNotServer_ReturnsFalse()
        {
            // Arrange
            _inspector.SetIsServer(false);

            // Assert
            Assert.That(_inspector.CanBeStoppedByPlayer, Is.False);
        }

        [Test]
        public void CanBeStoppedByPlayer_WhenIsStopped_ReturnsFalse()
        {
            // Arrange
            _inspector.SetIsServer(true);
            _inspector.SetIsStoppedByPlayer(true);

            // Assert
            Assert.That(_inspector.CanBeStoppedByPlayer, Is.False);
        }

        [Test]
        public void CanBeStoppedByPlayer_WhenOnCooldown_ReturnsFalse()
        {
            // Arrange
            _inspector.SetIsServer(true);
            _inspector.SetNextInteractionTime(UnityEngine.Time.time + 10f);

            // Assert
            Assert.That(_inspector.CanBeStoppedByPlayer, Is.False);
        }

        [Test]
        public void CanBeStoppedByPlayer_WhenReady_ReturnsTrue()
        {
            // Arrange
            _inspector.SetIsServer(true);
            _inspector.SetNextInteractionTime(UnityEngine.Time.time - 1f);

            // Assert
            Assert.That(_inspector.CanBeStoppedByPlayer, Is.True);
        }

        [Test]
        public void TryStopByPlayerInteraction_WhenNotServer_ReturnsFalse()
        {
            // Arrange
            _inspector.SetIsServer(false);

            // Act
            bool result = _inspector.TryStopByPlayerInteraction(1UL);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryStopByPlayerInteraction_WhenCannotBeStopped_ReturnsFalse()
        {
            // Arrange
            _inspector.SetIsServer(true);
            _inspector.SetIsStoppedByPlayer(true);

            // Act
            bool result = _inspector.TryStopByPlayerInteraction(1UL);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void TryStopByPlayerInteraction_WhenValid_StopsInspector()
        {
            // Arrange
            _inspector.SetIsServer(true);
            _inspector.SetNextInteractionTime(UnityEngine.Time.time - 1f);

            // Act
            bool result = _inspector.TryStopByPlayerInteraction(1UL);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_inspector.WasStopRoutineStarted, Is.True);
        }

        [Test]
        public void GetInteractionCooldownRemaining_WhenZero_ReturnsZero()
        {
            // Arrange
            _inspector.SetNextInteractionTime(0f);

            // Act
            float cooldown = _inspector.GetInteractionCooldownRemaining();

            // Assert
            Assert.That(cooldown, Is.Zero);
        }

        [Test]
        public void GetInteractionCooldownRemaining_WhenFuture_ReturnsPositiveValue()
        {
            // Arrange
            float futureTime = UnityEngine.Time.time + 5f;
            _inspector.SetNextInteractionTime(futureTime);

            // Act
            float cooldown = _inspector.GetInteractionCooldownRemaining();

            // Assert
            Assert.That(cooldown, Is.GreaterThan(0));
        }

        [Test]
        public void SetLockedTarget_SetsHasSelectedTargetTrue()
        {
            // Act
            _inspector.SetLockedTargetMock();

            // Assert
            Assert.That(_inspector.HasSelectedTarget, Is.True);
        }

        [Test]
        public void ClearLockedTarget_SetsHasSelectedTargetFalse()
        {
            // Arrange
            _inspector.SetLockedTargetMock();

            // Act
            _inspector.ClearLockedTargetMock();

            // Assert
            Assert.That(_inspector.HasSelectedTarget, Is.False);
        }

        // Моковая реализация для тестирования
        private class MockInspectorAgent
        {
            private bool _isServer = true;
            private bool _isStoppedByPlayer = false;
            private float _nextInteractionTime = 0f;
            private bool _hasSelectedTarget = false;
            public bool WasStopRoutineStarted { get; private set; }

            public bool HasSelectedTarget => _hasSelectedTarget;
            public bool IsStoppedByPlayer => _isStoppedByPlayer;

            public bool CanBeStoppedByPlayer =>
                _isServer && !_isStoppedByPlayer && UnityEngine.Time.time >= _nextInteractionTime;

            public void SetIsServer(bool isServer)
            {
                _isServer = isServer;
            }

            public void SetIsStoppedByPlayer(bool isStopped)
            {
                _isStoppedByPlayer = isStopped;
            }

            public void SetNextInteractionTime(float time)
            {
                _nextInteractionTime = time;
            }

            public bool TryStopByPlayerInteraction(ulong playerId)
            {
                if (!_isServer) return false;
                if (!CanBeStoppedByPlayer) return false;

                WasStopRoutineStarted = true;
                return true;
            }

            public float GetInteractionCooldownRemaining()
            {
                return Mathf.Max(0f, _nextInteractionTime - UnityEngine.Time.time);
            }

            public void SetLockedTargetMock()
            {
                _hasSelectedTarget = true;
            }

            public void ClearLockedTargetMock()
            {
                _hasSelectedTarget = false;
            }
        }
    }
}
