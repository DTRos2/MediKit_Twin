using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class StandReachProbeClearanceTests
    {
        private static readonly Pose ArmBaseAtOrigin = new Pose(Vector3.zero, Quaternion.identity);

        private static StandCandidate At(Vector3 armBasePosition) => new StandCandidate(armBasePosition, 0f, 0f);

        [Test]
        public void WithoutAClearanceQuestion_NoClearanceFunctionIsOffered()
        {
            var probe = new StandReachProbe(pose => true, ArmBaseAtOrigin, 0f, 0f);

            Assert.That(probe.ClearAt(At(Vector3.zero)), Is.Null);
        }

        [Test]
        public void TheClearanceQuestion_GetsTheTargetAsSeenFromTheCurrentBase_AndTheBaseTheArmWouldStandAt()
        {
            var askedTargets = new List<Pose>();
            var askedBases = new List<Pose>();
            var probe = new StandReachProbe(
                pose => true,
                ArmBaseAtOrigin,
                0f,
                0f,
                (target, armBase) =>
                {
                    askedTargets.Add(target);
                    askedBases.Add(armBase);
                    return true;
                });

            // 후보 자리는 현재보다 1m 앞이다. 월드에서 그 자리 정면 0.5m인 목표는 지금 베이스 정면 0.5m로 옮겨 묻고,
            // 팔은 후보 자리(앞으로 1m)에 서 있는 것으로 겹침을 따진다.
            probe.ClearAt(At(new Vector3(0f, 0f, 1f)))(new Pose(new Vector3(0f, 1f, 1.5f), Quaternion.identity));

            Assert.That(askedTargets.Count, Is.EqualTo(1));
            Assert.That(Vector3.Distance(askedTargets[0].position, new Vector3(0f, 1f, 0.5f)), Is.LessThan(1e-4f));
            Assert.That(Vector3.Distance(askedBases[0].position, new Vector3(0f, 0f, 1f)), Is.LessThan(1e-4f));
        }

        [Test]
        public void TheAnswerOfTheClearanceQuestionIsPassedThrough()
        {
            var clear = new StandReachProbe(pose => true, ArmBaseAtOrigin, 0f, 0f, (target, armBase) => true).ClearAt(At(Vector3.zero));
            var blocked = new StandReachProbe(pose => true, ArmBaseAtOrigin, 0f, 0f, (target, armBase) => false).ClearAt(At(Vector3.zero));
            var pose2 = new Pose(Vector3.one, Quaternion.identity);

            Assert.That(clear(pose2), Is.True);
            Assert.That(blocked(pose2), Is.False);
        }

        [Test]
        public void TheReachQuestionIsNotAffectedByTheClearanceQuestion()
        {
            var probe = new StandReachProbe(pose => true, ArmBaseAtOrigin, 0f, 0f, (target, armBase) => false);

            Assert.That(probe.At(At(Vector3.zero))(new Pose(Vector3.one, Quaternion.identity)), Is.True);
        }
    }
}
