using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class FingerDriveBoosterTests
    {
        private GameObject _root;
        private ArticulationBody[] _fingers;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("robot");
            _root.AddComponent<ArticulationBody>();

            _fingers = new ArticulationBody[2];
            for (int i = 0; i < _fingers.Length; i++)
            {
                var child = new GameObject($"finger{i}");
                child.transform.SetParent(_root.transform, false);
                ArticulationBody body = child.AddComponent<ArticulationBody>();
                body.jointType = ArticulationJointType.PrismaticJoint;
                body.xDrive = new ArticulationDrive
                {
                    stiffness = 10000f,
                    damping = 100f,
                    forceLimit = 100f,
                    target = 0.01f,
                    lowerLimit = 0f,
                    upperLimit = 0.04f,
                };
                _fingers[i] = body;
            }
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_root);
        }

        [Test]
        public void Scale_MultipliesStiffnessDampingAndForceLimit_AndKeepsTheRest()
        {
            var drive = new ArticulationDrive { stiffness = 10f, damping = 2f, forceLimit = 5f, target = 0.3f, lowerLimit = -1f, upperLimit = 1f };

            ArticulationDrive scaled = FingerDriveBooster.Scale(drive, 10f);

            Assert.That(scaled.stiffness, Is.EqualTo(100f));
            Assert.That(scaled.damping, Is.EqualTo(20f));
            Assert.That(scaled.forceLimit, Is.EqualTo(50f));
            Assert.That(scaled.target, Is.EqualTo(0.3f));
            Assert.That(scaled.lowerLimit, Is.EqualTo(-1f));
            Assert.That(scaled.upperLimit, Is.EqualTo(1f));
        }

        [Test]
        public void Boost_RaisesTheDriveOfEveryFinger()
        {
            var booster = new FingerDriveBooster(_fingers, 10f);

            booster.Boost();

            Assert.That(booster.IsBoosted, Is.True);
            foreach (ArticulationBody finger in _fingers)
            {
                Assert.That(finger.xDrive.stiffness, Is.EqualTo(100000f));
                Assert.That(finger.xDrive.damping, Is.EqualTo(1000f));
                Assert.That(finger.xDrive.forceLimit, Is.EqualTo(1000f));
            }
        }

        [Test]
        public void Boost_DoesNotChangeTheDriveTarget()
        {
            var booster = new FingerDriveBooster(_fingers, 10f);

            booster.Boost();

            Assert.That(_fingers[0].xDrive.target, Is.EqualTo(0.01f).Within(1e-6f));
        }

        [Test]
        public void Restore_BringsBackTheOriginalDrive()
        {
            var booster = new FingerDriveBooster(_fingers, 10f);
            booster.Boost();

            booster.Restore();

            Assert.That(booster.IsBoosted, Is.False);
            foreach (ArticulationBody finger in _fingers)
            {
                Assert.That(finger.xDrive.stiffness, Is.EqualTo(10000f));
                Assert.That(finger.xDrive.damping, Is.EqualTo(100f));
                Assert.That(finger.xDrive.forceLimit, Is.EqualTo(100f));
            }
        }

        [Test]
        public void Restore_KeepsATargetChangedWhileBoosted()
        {
            var booster = new FingerDriveBooster(_fingers, 10f);
            booster.Boost();
            ArticulationDrive drive = _fingers[0].xDrive;
            drive.target = 0.03f;
            _fingers[0].xDrive = drive;

            booster.Restore();

            Assert.That(_fingers[0].xDrive.target, Is.EqualTo(0.03f).Within(1e-6f));
        }

        [Test]
        public void Boost_CalledTwice_DoesNotSaveTheBoostedDriveAsTheOriginal()
        {
            var booster = new FingerDriveBooster(_fingers, 10f);
            booster.Boost();

            booster.Boost();
            booster.Restore();

            Assert.That(_fingers[0].xDrive.stiffness, Is.EqualTo(10000f));
        }

        [Test]
        public void Restore_WithoutBoost_DoesNothing()
        {
            var booster = new FingerDriveBooster(_fingers, 10f);

            booster.Restore();

            Assert.That(_fingers[0].xDrive.stiffness, Is.EqualTo(10000f));
        }

        [Test]
        public void Constructor_RejectsMissingJointsAndMultipliersBelowOne()
        {
            Assert.Throws<ArgumentNullException>(() => new FingerDriveBooster(null, 10f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FingerDriveBooster(_fingers, 0.5f));
        }
    }
}
