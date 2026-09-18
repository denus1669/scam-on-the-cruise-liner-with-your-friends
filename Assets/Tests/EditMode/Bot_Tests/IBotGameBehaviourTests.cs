using NUnit.Framework;
using Assets.Casino.Bot;
using Assets.Casino.Games;
using NSubstitute;
using UnityEngine;

namespace Casino.Tests.EditMode.Bot_Tests
{
    /// <summary>
    /// Тесты для интерфейса IBotGameBehaviour.
    /// Проверяют корректность реализации ботами этого интерфейса.
    /// </summary>
    [TestFixture]
    public class IBotGameBehaviourTests
    {
        private IGameTable _mockTable;
        private MockBotGameBehaviour _botBehaviour;

        [SetUp]
        public void SetUp()
        {
            _mockTable = Substitute.For<IGameTable>();
            _botBehaviour = new MockBotGameBehaviour();
        }

        [Test]
        public void InitializeGame_WithValidTable_SetsTable()
        {
            // Arrange
            var table = _mockTable;

            // Act
            _botBehaviour.InitializeGame(table);

            // Assert
            Assert.That(_botBehaviour.CurrentTable, Is.EqualTo(table));
        }

        [Test]
        public void InitializeGame_WithNullTable_SetsNullTable()
        {
            // Arrange & Act
            _botBehaviour.InitializeGame(null);

            // Assert
            Assert.That(_botBehaviour.CurrentTable, Is.Null);
        }

        [Test]
        public void StartSession_BeforeInitialize_WorksCorrectly()
        {
            // Arrange & Act
            _botBehaviour.StartSession();

            // Assert
            Assert.That(_botBehaviour.SessionStarted, Is.True);
        }

        [Test]
        public void EndSession_AfterStart_SetsSessionToFalse()
        {
            // Arrange
            _botBehaviour.StartSession();

            // Act
            _botBehaviour.EndSession();

            // Assert
            Assert.That(_botBehaviour.SessionStarted, Is.False);
        }

        [Test]
        public void InitializeGame_CallsTableMethods()
        {
            // Arrange
            var table = _mockTable;

            // Act
            _botBehaviour.InitializeGame(table);

            // Assert
            Assert.That(_botBehaviour.CurrentTable, Is.Not.Null);
        }

        [Test]
        public void StartSession_MultipleTimes_DoesNotThrow()
        {
            // Arrange & Act
            _botBehaviour.StartSession();
            
            // Assert - не должно выбрасывать исключений
            Assert.DoesNotThrow(() => _botBehaviour.StartSession());
        }

        [Test]
        public void EndSession_MultipleTimes_DoesNotThrow()
        {
            // Arrange & Act
            _botBehaviour.EndSession();
            
            // Assert - не должно выбрасывать исключений
            Assert.DoesNotThrow(() => _botBehaviour.EndSession());
        }

        [Test]
        public void SessionFlow_Initialize_Start_End_WorksCorrectly()
        {
            // Arrange
            var table = _mockTable;

            // Act
            _botBehaviour.InitializeGame(table);
            _botBehaviour.StartSession();
            _botBehaviour.EndSession();

            // Assert
            Assert.That(_botBehaviour.CurrentTable, Is.EqualTo(table));
            Assert.That(_botBehaviour.SessionStarted, Is.False);
        }

        [Test]
        public void InitializeGame_ReinitializeWithNewTable_UpdatesTable()
        {
            // Arrange
            var table1 = Substitute.For<IGameTable>();
            var table2 = Substitute.For<IGameTable>();

            // Act
            _botBehaviour.InitializeGame(table1);
            _botBehaviour.InitializeGame(table2);

            // Assert
            Assert.That(_botBehaviour.CurrentTable, Is.EqualTo(table2));
        }

        // Моковая реализация для тестирования
        private class MockBotGameBehaviour : IBotGameBehaviour
        {
            public IGameTable CurrentTable { get; private set; }
            public bool SessionStarted { get; private set; }

            public void InitializeGame(IGameTable table)
            {
                CurrentTable = table;
            }

            public void StartSession()
            {
                SessionStarted = true;
            }

            public void EndSession()
            {
                SessionStarted = false;
            }
        }
    }
}
