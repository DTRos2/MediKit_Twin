using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class MultiStartInverseKinematicsTests
    {
        private const int JointCount = 2;
        private const int Restarts = 6;
        private const int Seed = 7;
        private const float FarAngle = 3f;
        private const float NearAngle = 0.2f;
        private const float TravelLimit = 1f;

        // 첫 풀이(현재 자세에서 시작)와 이후 재시작 풀이가 각각 어떤 해를 돌려줄지 정해 둔 가짜 IK.
        private sealed class ScriptedIk : IInverseKinematics
        {
            private readonly float _firstAngle;
            private readonly float _laterAngle;
            private readonly bool _laterConverges;

            internal int Calls;

            internal ScriptedIk(float firstAngle, float laterAngle, bool laterConverges)
            {
                _firstAngle = firstAngle;
                _laterAngle = laterAngle;
                _laterConverges = laterConverges;
            }

            public IkResult Solve(Pose targetInBase, float[] seedRad, float[] resultRad)
            {
                bool isFirst = Calls++ == 0;
                float angle = isFirst ? _firstAngle : _laterAngle;
                for (int i = 0; i < resultRad.Length; i++)
                {
                    resultRad[i] = angle;
                }

                bool converged = isFirst || _laterConverges;
                return new IkResult(converged, converged ? 0f : 0.1f, 0f, 1);
            }
        }

        private static KinematicChain Chain()
        {
            var joints = new KinematicChain.JointSpec[JointCount];
            for (int i = 0; i < JointCount; i++)
            {
                joints[i] = new KinematicChain.JointSpec(
                    Vector3.up, Quaternion.identity, Vector3.forward, Vector3.zero, -Mathf.PI, Mathf.PI);
            }

            return new KinematicChain(joints, Vector3.up, Quaternion.identity);
        }

        private static IkResult Solve(ScriptedIk inner, float travelLimitRad, float[] result)
        {
            var solver = new MultiStartInverseKinematics(inner, Chain(), Restarts, Seed, travelLimitRad);
            return solver.Solve(Pose.identity, new float[JointCount], result);
        }

        [Test]
        public void WithoutALimit_TheFirstConvergedSolutionIsUsedEvenIfItIsFar()
        {
            var inner = new ScriptedIk(FarAngle, NearAngle, true);
            var result = new float[JointCount];

            IkResult ik = Solve(inner, 0f, result);

            Assert.That(ik.Converged, Is.True);
            Assert.That(result[0], Is.EqualTo(FarAngle));
            Assert.That(inner.Calls, Is.EqualTo(1));
        }

        [Test]
        public void WithALimit_AFarSolutionIsSkipped_AndANearRestartSolutionIsUsed()
        {
            var inner = new ScriptedIk(FarAngle, NearAngle, true);
            var result = new float[JointCount];

            IkResult ik = Solve(inner, TravelLimit, result);

            Assert.That(ik.Converged, Is.True);
            Assert.That(result[0], Is.EqualTo(NearAngle));
            Assert.That(inner.Calls, Is.EqualTo(1 + Restarts), "더 덜 움직이는 해를 찾으려고 재시작을 모두 해 본다");
        }

        [Test]
        public void WithALimit_WhenOnlyFarSolutionsExist_ItReportsNotConverged()
        {
            var inner = new ScriptedIk(FarAngle, FarAngle, true);
            var result = new float[JointCount];

            IkResult ik = Solve(inner, TravelLimit, result);

            Assert.That(ik.Converged, Is.False);
        }

        [Test]
        public void WithALimit_ANearFirstSolutionReturnsImmediately()
        {
            var inner = new ScriptedIk(NearAngle, FarAngle, true);
            var result = new float[JointCount];

            IkResult ik = Solve(inner, TravelLimit, result);

            Assert.That(ik.Converged, Is.True);
            Assert.That(inner.Calls, Is.EqualTo(1));
        }

        [Test]
        public void SameInput_GivesTheSameResultEveryTime()
        {
            var first = new float[JointCount];
            var second = new float[JointCount];

            Solve(new ScriptedIk(FarAngle, NearAngle, true), TravelLimit, first);
            Solve(new ScriptedIk(FarAngle, NearAngle, true), TravelLimit, second);

            Assert.That(second, Is.EqualTo(first));
        }
    }
}
