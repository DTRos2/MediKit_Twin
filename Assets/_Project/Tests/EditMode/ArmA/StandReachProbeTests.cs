using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class StandReachProbeTests
    {
        private static readonly Pose ArmBaseAtOrigin = new Pose(Vector3.zero, Quaternion.identity);

        private static StandCandidate At(Vector3 armBasePosition) => new StandCandidate(armBasePosition, 0f, 0f);

        [Test]
        public void TheCurrentArmIsAskedAboutTheTargetAsSeenFromTheCandidateStand()
        {
            var asked = new List<Pose>();
            var probe = new StandReachProbe(pose => { asked.Add(pose); return true; }, ArmBaseAtOrigin, 0f, 0f);

            // 후보 자리는 현재보다 1m 앞이다. 월드에서 그 자리 정면 0.5m인 목표는 지금 베이스 정면 0.5m로 물어야 한다.
            probe.At(At(new Vector3(0f, 0f, 1f)))(new Pose(new Vector3(0f, 1f, 1.5f), Quaternion.identity));

            Assert.That(asked.Count, Is.EqualTo(1));
            Assert.That(Vector3.Distance(asked[0].position, new Vector3(0f, 1f, 0.5f)), Is.LessThan(1e-4f));
        }

        [Test]
        public void WhenTheCandidateStandFacesAnotherWay_TheTargetIsAskedFromThatSide()
        {
            var asked = new List<Pose>();
            var probe = new StandReachProbe(pose => { asked.Add(pose); return true; }, ArmBaseAtOrigin, 0f, 90f);

            // 90도 돌아선 자리에서는 월드 +X 쪽 목표가 정면에 있다.
            probe.At(At(Vector3.zero))(new Pose(new Vector3(1f, 0f, 0f), Quaternion.identity));

            Assert.That(Vector3.Distance(asked[0].position, new Vector3(0f, 0f, 1f)), Is.LessThan(1e-4f));
        }

        [Test]
        public void TheAnswerOfTheCurrentArmIsPassedThrough()
        {
            var reaches = new StandReachProbe(pose => true, ArmBaseAtOrigin, 0f, 0f).At(At(Vector3.zero));
            var cannot = new StandReachProbe(pose => false, ArmBaseAtOrigin, 0f, 0f).At(At(Vector3.zero));
            var target = new Pose(Vector3.one, Quaternion.identity);

            Assert.That(reaches(target), Is.True);
            Assert.That(cannot(target), Is.False);
        }

        [Test]
        public void EachCandidateGetsItsOwnQuestion()
        {
            var asked = new List<Vector3>();
            var probe = new StandReachProbe(pose => { asked.Add(pose.position); return true; }, ArmBaseAtOrigin, 0f, 0f);
            var target = new Pose(new Vector3(0f, 0f, 2f), Quaternion.identity);

            probe.At(At(new Vector3(0f, 0f, 1f)))(target);
            probe.At(At(new Vector3(0f, 0f, 2f)))(target);

            Assert.That(asked[0].z, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(asked[1].z, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void Constructor_NullProbe_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new StandReachProbe(null, ArmBaseAtOrigin, 0f, 0f));
        }
    }
}
