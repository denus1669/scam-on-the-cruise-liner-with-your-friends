using NUnit.Framework;
using System;

namespace Casino.Tests.EditMode.Attention_Tests
{
    /// <summary>
    /// Тесты для PlayerAttentionController (чистая логика без Unity зависимостей).
    /// </summary>
    [TestFixture]
    public class PlayerAttentionControllerLogicTests
    {
        private MockPlayerAttentionController _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new MockPlayerAttentionController();
        }

        [Test]
        public void IsAttentionActive_Initially_ReturnsFalse()
        {
            // Assert
            Assert.That(_controller.IsAttentionActive, Is.False);
        }

        [Test]
        public void ToggleAttentionLocal_WhenInactive_SetsActiveTrue()
        {
            // Arrange
            _controller.SetIsOwner(true);

            // Act
            _controller.ToggleAttentionLocal();

            // Assert
            Assert.That(_controller.IsAttentionActive, Is.True);
        }

        [Test]
        public void ToggleAttentionLocal_WhenActive_SetsActiveFalse()
        {
            // Arrange
            _controller.SetIsOwner(true);
            _controller.ToggleAttention(true);

            // Act
            _controller.ToggleAttentionLocal();

            // Assert
            Assert.That(_controller.IsAttentionActive, Is.False);
        }

        [Test]
        public void ToggleAttention_WithTrueParameter_SetsActiveTrue()
        {
            // Act
            _controller.ToggleAttention(true);

            // Assert
            Assert.That(_controller.IsAttentionActive, Is.True);
        }

        [Test]
        public void ToggleAttention_WithFalseParameter_SetsActiveFalse()
        {
            // Arrange
            _controller.ToggleAttention(true);

            // Act
            _controller.ToggleAttention(false);

            // Assert
            Assert.That(_controller.IsAttentionActive, Is.False);
        }

        [Test]
        public void OnLocalAttentionChanged_Event_RaisedOnToggle()
        {
            // Arrange
            _controller.SetIsOwner(true);
            bool eventRaised = false;
            bool capturedState = false;
            _controller.OnLocalAttentionChanged += (state) =>
            {
                eventRaised = true;
                capturedState = state;
            };

            // Act
            _controller.ToggleAttention(true);

            // Assert
            Assert.That(eventRaised, Is.True);
            Assert.That(capturedState, Is.True);
        }

        [Test]
        public void OnGlobalAttentionChanged_Event_RaisedOnToggle()
        {
            // Arrange
            bool eventRaised = false;
            bool capturedState = false;
            _controller.OnGlobalAttentionChanged += (state) =>
            {
                eventRaised = true;
                capturedState = state;
            };

            // Act
            _controller.ToggleAttention(true);

            // Assert
            Assert.That(eventRaised, Is.True);
            Assert.That(capturedState, Is.True);
        }

        [Test]
        public void ToggleAttentionLocal_WhenNotOwner_DoesNothing()
        {
            // Arrange
            _controller.SetIsOwner(false);

            // Act
            _controller.ToggleAttentionLocal();

            // Assert
            Assert.That(_controller.IsAttentionActive, Is.False);
        }

        [Test]
        public void MultipleToggles_AlternateStateCorrectly()
        {
            // Arrange
            _controller.SetIsOwner(true);

            // Act & Assert
            _controller.ToggleAttentionLocal();
            Assert.That(_controller.IsAttentionActive, Is.True);

            _controller.ToggleAttentionLocal();
            Assert.That(_controller.IsAttentionActive, Is.False);

            _controller.ToggleAttentionLocal();
            Assert.That(_controller.IsAttentionActive, Is.True);
        }

        // Моковая реализация для тестирования
        private class MockPlayerAttentionController
        {
            private bool _isAttention = false;
            private bool _isOwner = true;

            public event Action<bool> OnLocalAttentionChanged;
            public event Action<bool> OnGlobalAttentionChanged;

            public bool IsAttentionActive => _isAttention;

            public void SetIsOwner(bool isOwner)
            {
                _isOwner = isOwner;
            }

            public void ToggleAttentionLocal()
            {
                if (!_isOwner) return;
                ToggleAttention(!_isAttention);
            }

            public void ToggleAttention(bool newState)
            {
                bool previous = _isAttention;
                _isAttention = newState;
                HandleAttentionStateChanged(previous, newState);
            }

            private void HandleAttentionStateChanged(bool previous, bool current)
            {
                OnGlobalAttentionChanged?.Invoke(current);
                if (_isOwner)
                {
                    OnLocalAttentionChanged?.Invoke(current);
                }
            }
        }
    }
}
