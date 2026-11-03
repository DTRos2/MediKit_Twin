using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class FingerJointSetTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("robot");
            _root.AddComponent<ArticulationBody>();
            AddFinger("finger_a", ArticulationJointType.PrismaticJoint, withCollider: true);
            AddFinger("finger_b", ArticulationJointType.RevoluteJoint, withCollider: false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void TryCreate_FindsEveryJointByName_AndKeepsTheProfileValues()
        {
            var host = new FakeHost(_root.transform, new ArmProfileSO.GripperJointEntry("finger_a", 0f, 0.04f), new ArmProfileSO.GripperJointEntry("finger_b", 0.1f, 0.9f));

            bool created = FingerJointSet.TryCreate(host, out FingerJointSet set, out string error);

            Assert.That(created, Is.True, error);
            Assert.That(set.Count, Is.EqualTo(2));
            Assert.That(set.Joints[0].name, Is.EqualTo("finger_a"));
            Assert.That(set.Joints[1].name, Is.EqualTo("finger_b"));
            Assert.That(set.OpenValues[1], Is.EqualTo(0.1f));
            Assert.That(set.ClosedValues[0], Is.EqualTo(0.04f));
        }

        [Test]
        public void TryCreate_CollectsTheCollidersUnderTheJoints()
        {
            var host = new FakeHost(_root.transform, new ArmProfileSO.GripperJointEntry("finger_a", 0f, 0.04f), new ArmProfileSO.GripperJointEntry("finger_b", 0f, 1f));

            FingerJointSet.TryCreate(host, out FingerJointSet set, out _);

            Assert.That(set.Colliders.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryCreate_Fails_AndNamesTheMissingLink()
        {
            var host = new FakeHost(_root.transform, new ArmProfileSO.GripperJointEntry("finger_a", 0f, 0.04f), new ArmProfileSO.GripperJointEntry("missing_link", 0f, 1f));

            bool created = FingerJointSet.TryCreate(host, out FingerJointSet set, out string error);

            Assert.That(created, Is.False);
            Assert.That(set, Is.Null);
            Assert.That(error, Does.Contain("missing_link"));
        }

        [Test]
        public void IsRevolute_FollowsTheJointType()
        {
            var host = new FakeHost(_root.transform, new ArmProfileSO.GripperJointEntry("finger_a", 0f, 0.04f), new ArmProfileSO.GripperJointEntry("finger_b", 0f, 1f));
            FingerJointSet.TryCreate(host, out FingerJointSet set, out _);

            Assert.That(set.IsRevolute(0), Is.False);
            Assert.That(set.IsRevolute(1), Is.True);
        }

        private void AddFinger(string name, ArticulationJointType jointType, bool withCollider)
        {
            var finger = new GameObject(name);
            finger.transform.SetParent(_root.transform, false);
            ArticulationBody body = finger.AddComponent<ArticulationBody>();
            body.jointType = jointType;
            if (withCollider)
            {
                finger.AddComponent<BoxCollider>();
            }
        }

        private sealed class FakeHost : IGripperHost
        {
            public FakeHost(Transform robotRoot, params ArmProfileSO.GripperJointEntry[] joints)
            {
                RobotRoot = robotRoot;
                GripperJoints = new List<ArmProfileSO.GripperJointEntry>(joints);
            }

            public Transform RobotRoot { get; }

            public IReadOnlyList<ArmProfileSO.GripperJointEntry> GripperJoints { get; }

            public ArticulationBody ToolBody => null;

            public Pose ToolPose => Pose.identity;
        }
    }
}
