using NUnit.Framework;
using Assets.Casino.Cheating;

namespace Casino.Tests.EditMode.Cheating_Tests
{
    /// <summary>
    /// Тесты для интерфейса ICheatVisualIndicator.
    /// Проверяют корректность реализации индикаторами мухлежа.
    /// </summary>
    [TestFixture]
    public class ICheatVisualIndicatorTests
    {
        private MockCheatVisualIndicator _indicator;

        [SetUp]
        public void SetUp()
        {
            _indicator = new MockCheatVisualIndicator();
        }

        [Test]
        public void ShowCheatIndicator_Called_SetsVisibleTrue()
        {
            // Act
            _indicator.ShowCheatIndicator();

            // Assert
            Assert.That(_indicator.IsVisible, Is.True);
            Assert.That(_indicator.ShowCallCount, Is.EqualTo(1));
        }

        [Test]
        public void HideCheatIndicator_Called_SetsVisibleFalse()
        {
            // Act
            _indicator.HideCheatIndicator();

            // Assert
            Assert.That(_indicator.IsVisible, Is.False);
            Assert.That(_indicator.HideCallCount, Is.EqualTo(1));
        }

        [Test]
        public void ShowCheatIndicator_MultipleCalls_IncrementsCount()
        {
            // Act
            _indicator.ShowCheatIndicator();
            _indicator.ShowCheatIndicator();
            _indicator.ShowCheatIndicator();

            // Assert
            Assert.That(_indicator.ShowCallCount, Is.EqualTo(3));
            Assert.That(_indicator.IsVisible, Is.True);
        }

        [Test]
        public void HideCheatIndicator_MultipleCalls_IncrementsCount()
        {
            // Act
            _indicator.HideCheatIndicator();
            _indicator.HideCheatIndicator();

            // Assert
            Assert.That(_indicator.HideCallCount, Is.EqualTo(2));
            Assert.That(_indicator.IsVisible, Is.False);
        }

        [Test]
        public void ShowThenHide_Sequentially_WorksCorrectly()
        {
            // Act
            _indicator.ShowCheatIndicator();
            _indicator.HideCheatIndicator();

            // Assert
            Assert.That(_indicator.ShowCallCount, Is.EqualTo(1));
            Assert.That(_indicator.HideCallCount, Is.EqualTo(1));
            Assert.That(_indicator.IsVisible, Is.False);
        }

        [Test]
        public void HideThenShow_Sequentially_WorksCorrectly()
        {
            // Act
            _indicator.HideCheatIndicator();
            _indicator.ShowCheatIndicator();

            // Assert
            Assert.That(_indicator.ShowCallCount, Is.EqualTo(1));
            Assert.That(_indicator.HideCallCount, Is.EqualTo(1));
            Assert.That(_indicator.IsVisible, Is.True);
        }

        [Test]
        public void Toggle_ShowHideShow_EndsVisible()
        {
            // Act
            _indicator.ShowCheatIndicator();
            _indicator.HideCheatIndicator();
            _indicator.ShowCheatIndicator();

            // Assert
            Assert.That(_indicator.IsVisible, Is.True);
            Assert.That(_indicator.ShowCallCount, Is.EqualTo(2));
            Assert.That(_indicator.HideCallCount, Is.EqualTo(1));
        }

        // Моковая реализация для тестирования
        private class MockCheatVisualIndicator : ICheatVisualIndicator
        {
            public bool IsVisible { get; private set; }
            public int ShowCallCount { get; private set; }
            public int HideCallCount { get; private set; }

            public void ShowCheatIndicator()
            {
                IsVisible = true;
                ShowCallCount++;
            }

            public void HideCheatIndicator()
            {
                IsVisible = false;
                HideCallCount++;
            }
        }
    }
}
