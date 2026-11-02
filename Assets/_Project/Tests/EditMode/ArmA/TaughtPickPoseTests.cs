using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class TaughtPickPoseTests
    {
        private static readonly float[] Joints = { -25.1f * Mathf.Deg2Rad, -30.7f * Mathf.Deg2Rad, 69.1f * Mathf.Deg2Rad };

        private static TaughtPickPose Capture(string slotId = "A-1") => TaughtPickPose.Capture(
            slotId,
            new Vector3(1.85f, 0.10f, 1.40f),
            12.5f,
            new Vector3(1.88f, 0.55f, 1.42f),
            new Pose(new Vector3(2.02f, 0.73f, 2.30f), Quaternion.Euler(10f, 20f, 30f)),
            Joints);

        [Test]
        public void KeepsTheSlotAndTheStandAndTheYaw()
        {
            TaughtPickPose pose = Capture();

            Assert.That(pose.SlotId, Is.EqualTo("A-1"));
            Assert.That(pose.RootYawDeg, Is.EqualTo(12.5f));
            Assert.That(pose.ArmBasePosition.x, Is.EqualTo(1.88f).Within(1e-5f));
            Assert.That(pose.ArmBasePosition.z, Is.EqualTo(1.42f).Within(1e-5f));
        }

        [Test]
        public void KeepsTheArmARootPosition()
        {
            TaughtPickPose pose = Capture();

            Assert.That(pose.RootPosition.x, Is.EqualTo(1.85f).Within(1e-5f));
            Assert.That(pose.RootPosition.z, Is.EqualTo(1.40f).Within(1e-5f));
        }

        [Test]
        public void TheStandIsOnTheFloor_TheHeightsAreDropped()
        {
            Assert.That(Capture().ArmBasePosition.y, Is.EqualTo(0f));
            Assert.That(Capture().RootPosition.y, Is.EqualTo(0f));
        }

        [Test]
        public void KeepsTheToolPoseInTheWorld()
        {
            Pose tool = Capture().ToolPose;

            Assert.That(Vector3.Distance(tool.position, new Vector3(2.02f, 0.73f, 2.30f)), Is.LessThan(1e-5f));
            Assert.That(Quaternion.Angle(tool.rotation, Quaternion.Euler(10f, 20f, 30f)), Is.LessThan(1e-3f));
        }

        [Test]
        public void JointsAreKeptAndGivenBackInRadians()
        {
            float[] back = Capture().JointsRad();

            Assert.That(back.Length, Is.EqualTo(Joints.Length));
            for (int i = 0; i < back.Length; i++)
            {
                Assert.That(back[i], Is.EqualTo(Joints[i]).Within(1e-5f));
            }
        }

        [Test]
        public void JointsRad_GivesANewArrayEachTime()
        {
            TaughtPickPose pose = Capture();

            float[] first = pose.JointsRad();
            first[0] = 99f;

            Assert.That(pose.JointsRad()[0], Is.Not.EqualTo(99f), "밖에서 고쳐도 저장된 값은 그대로다");
        }

        [Test]
        public void TheSlotIdIsNormalized()
        {
            Assert.That(Capture(" a-1 ").SlotId, Is.EqualTo("A-1"));
        }

        [Test]
        public void EmptySlotId_Throws()
        {
            Assert.Throws<ArgumentException>(() => Capture("  "));
        }

        [Test]
        public void NullJoints_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                TaughtPickPose.Capture("A-1", Vector3.zero, 0f, Vector3.zero, new Pose(Vector3.zero, Quaternion.identity), null));
        }
    }
}
