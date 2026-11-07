using System.Collections.Generic;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class StowedPoseReachProbeTests
    {
        private static readonly Pose Target = new Pose(new Vector3(1f, 2f, 3f), Quaternion.identity);

        private sealed class FakeStowSource : IArmStowPoseSource
        {
            internal bool HasStowPose = true;
            internal float[] Pose = { 0.1f, -0.5f, 1.2f };

            public bool TryGetStowPose(out float[] jointsRad)
            {
                jointsRad = HasStowPose ? Pose : null;
                return HasStowPose;
            }
        }

        private sealed class FakeSolver : ISeededToolSolver
        {
            internal bool Result = true;
            internal IReadOnlyList<float> SeedAsked;
            internal Pose PoseAsked;
            internal int Calls;

            public bool TrySolveFrom(Pose worldPose, IReadOnlyList<float> seedRad, float[] solutionRad)
            {
                Calls++;
                PoseAsked = worldPose;
                SeedAsked = seedRad;
                return Result;
            }
        }

        [Test]
        public void SolvesFromTheStowPose_WhenItIsKnown()
        {
            var source = new FakeStowSource();
            var solver = new FakeSolver();
            bool fallbackUsed = false;
            var probe = new StowedPoseReachProbe(source, solver, pose => fallbackUsed = true);

            bool result = probe.CanReach(Target);

            Assert.That(result, Is.True);
            Assert.That(fallbackUsed, Is.False);
            Assert.That(solver.SeedAsked, Is.EqualTo(source.Pose));
            Assert.That(solver.PoseAsked, Is.EqualTo(Target));
        }

        [Test]
        public void AnswersNo_WhenTheSolverCannotReachFromTheStowPose_EvenIfTheFallbackWould()
        {
            var solver = new FakeSolver { Result = false };
            var probe = new StowedPoseReachProbe(new FakeStowSource(), solver, pose => true);

            Assert.That(probe.CanReach(Target), Is.False);
        }

        [Test]
        public void UsesTheFallback_WhenThereIsNoStowPose()
        {
            var solver = new FakeSolver();
            var probe = new StowedPoseReachProbe(new FakeStowSource { HasStowPose = false }, solver, pose => true);

            Assert.That(probe.CanReach(Target), Is.True);
            Assert.That(solver.Calls, Is.EqualTo(0));
        }

        [Test]
        public void UsesTheFallback_WhenThereIsNoStowSourceOrNoSolver()
        {
            var withoutSource = new StowedPoseReachProbe(null, new FakeSolver(), pose => true);
            var withoutSolver = new StowedPoseReachProbe(new FakeStowSource(), null, pose => false);

            Assert.That(withoutSource.CanReach(Target), Is.True);
            Assert.That(withoutSolver.CanReach(Target), Is.False);
        }

        [Test]
        public void AsksTheSolverEveryTime_NotJustTheFirst()
        {
            var solver = new FakeSolver();
            var probe = new StowedPoseReachProbe(new FakeStowSource(), solver, pose => false);

            probe.CanReach(Target);
            solver.Result = false;

            Assert.That(probe.CanReach(Target), Is.False);
            Assert.That(solver.Calls, Is.EqualTo(2));
        }
    }
}
