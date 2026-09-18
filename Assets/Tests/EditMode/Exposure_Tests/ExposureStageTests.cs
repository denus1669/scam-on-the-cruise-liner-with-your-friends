using NUnit.Framework;
using Assets.Casino.Exposure;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Casino.Tests.EditMode.Exposure_Tests
{
    /// <summary>
    /// Тесты для ExposureStage.
    /// Проверяют инициализацию полей и работу с коллекциями объектов.
    /// </summary>
    [TestFixture]
    public class ExposureStageTests
    {
        private readonly List<GameObject> _spawnedObjects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawnedObjects)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
            _spawnedObjects.Clear();
        }

        private GameObject CreateTrackedObject(string name)
        {
            var go = new GameObject(name);
            _spawnedObjects.Add(go);
            return go;
        }

        // ---------- Инициализация по умолчанию ----------

        [Test]
        public void DefaultConstructor_LevelIsZero()
        {
            var stage = new ExposureStage();

            Assert.That(stage.level, Is.Zero);
        }

        [Test]
        public void DefaultConstructor_CollectionsAreNotNullAndEmpty()
        {
            var stage = new ExposureStage();

            Assert.That(stage.objectsToActivate, Is.Not.Null);
            Assert.That(stage.objectsToActivate, Is.Empty);

            Assert.That(stage.objectsToDeactivate, Is.Not.Null);
            Assert.That(stage.objectsToDeactivate, Is.Empty);
        }

        // ---------- Присваивание уровня ----------

        [Test]
        public void Level_CanBeAssigned(
            [Values(1, 2, 3)] int value)
        {
            var stage = new ExposureStage { level = value };

            Assert.That(stage.level, Is.EqualTo(value));
        }

        // ---------- Коллекции ----------

        [Test]
        public void ObjectsToActivate_Add_IncreasesCount()
        {
            var stage = new ExposureStage();
            var go = CreateTrackedObject("ActivateMe");

            stage.objectsToActivate.Add(go);

            Assert.That(stage.objectsToActivate, Has.Count.EqualTo(1));
            Assert.That(stage.objectsToActivate[0], Is.SameAs(go));
        }

        [Test]
        public void ObjectsToDeactivate_Add_IncreasesCount()
        {
            var stage = new ExposureStage();
            var go = CreateTrackedObject("DeactivateMe");

            stage.objectsToDeactivate.Add(go);

            Assert.That(stage.objectsToDeactivate, Has.Count.EqualTo(1));
            Assert.That(stage.objectsToDeactivate[0], Is.SameAs(go));
        }

        [Test]
        public void TwoInstances_HaveIndependentCollections()
        {
            var a = new ExposureStage();
            var b = new ExposureStage();
            var go = CreateTrackedObject("OnlyInA");

            a.objectsToActivate.Add(go);

            Assert.That(a.objectsToActivate, Has.Count.EqualTo(1));
            Assert.That(b.objectsToActivate, Is.Empty,
                "Коллекции разных экземпляров не должны быть общими.");
        }

        // ---------- UnityEvent ----------

        [Test]
        public void OnStageApplied_AssignedEvent_Invoke_CallsListeners()
        {
            var stage = new ExposureStage
            {
                onStageApplied = new UnityEvent()
            };

            int calls = 0;
            stage.onStageApplied.AddListener(() => calls++);

            stage.onStageApplied.Invoke();

            Assert.That(calls, Is.EqualTo(1));
        }
    }
}