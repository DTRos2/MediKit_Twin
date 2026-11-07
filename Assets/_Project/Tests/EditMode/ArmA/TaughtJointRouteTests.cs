using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class TaughtJointRouteTests
    {
        private static readonly float[] Taught = { 0.1f, 0.2f, 0.3f };

        private static readonly Pose Near = new Pose(new Vector3(0f, 0f, 1f), Quaternion.identity);
        private static readonly Pose Grasp = new Pose(new Vector3(0f, 0f, 2f), Quaternion.identity);
        private static readonly Pose Retreat = new Pose(new Vector3(0f, 0.1f, 1.5f), Quaternion.identity);

        // 자세마다 정해 둔 해를 돌려준다. 어떤 출발 자세로 불렀는지도 적어 둔다.
        private sealed class FakeSolver
        {
            internal readonly Dictionary<Vector3, float[]> Solutions = new Dictionary<Vector3, float[]>();
            internal readonly List<(Vector3 Position, float[] Seed)> Calls = new List<(Vector3, float[])>();

            internal float[] Solve(Pose pose, IReadOnlyList<float> seed)
            {
                var copy = new float[seed.Count];
                for (int i = 0; i < copy.Length; i++)
                {
                    copy[i] = seed[i];
                }

                Calls.Add((pose.position, copy));
                return Solutions.TryGetValue(pose.position, out float[] solution) ? solution : null;
            }
        }

        private static FakeSolver SolvingEverything() => new FakeSolver
        {
            Solutions =
            {
                [Grasp.position] = new[] { 1f, 1f, 1f },
                [Near.position] = new[] { 2f, 2f, 2f },
                [Retreat.position] = new[] { 3f, 3f, 3f }
            }
        };

        private static bool Build(FakeSolver solver, out TaughtJointRoute route) =>
            TaughtJointRoute.TryBuild(Taught, Near, Grasp, Retreat, solver.Solve, out route);

        [Test]
        public void WhenEveryPlaceIsSolved_TheRouteHoldsTheirJoints()
        {
            bool isBuilt = Build(SolvingEverything(), out TaughtJointRoute route);

            Assert.That(isBuilt, Is.True);
            Assert.That(route.Near, Is.EqualTo(new[] { 2f, 2f, 2f }));
            Assert.That(route.Grasp, Is.EqualTo(new[] { 1f, 1f, 1f }));
            Assert.That(route.Retreat, Is.EqualTo(new[] { 3f, 3f, 3f }));
        }

        [Test]
        public void TheGraspIsSolvedFromTheTaughtJoints()
        {
            FakeSolver solver = SolvingEverything();

            Build(solver, out _);

            var graspCall = solver.Calls.Find(call => call.Position == Grasp.position);
            Assert.That(graspCall.Seed, Is.EqualTo(Taught), "알려 준 관절 모양에서 출발해야 같은 모양으로 닿는다");
        }

        [Test]
        public void NearAndRetreatAreSolvedFromTheGraspSolution_SoTheShapeStaysTheSame()
        {
            FakeSolver solver = SolvingEverything();

            Build(solver, out _);

            Assert.That(solver.Calls.Find(call => call.Position == Near.position).Seed, Is.EqualTo(new[] { 1f, 1f, 1f }));
            Assert.That(solver.Calls.Find(call => call.Position == Retreat.position).Seed, Is.EqualTo(new[] { 1f, 1f, 1f }));
        }

        [Test]
        public void WhenTheGraspCannotBeSolved_NoRouteAndNoFurtherSolving()
        {
            FakeSolver solver = SolvingEverything();
            solver.Solutions.Remove(Grasp.position);

            bool isBuilt = Build(solver, out TaughtJointRoute route);

            Assert.That(isBuilt, Is.False);
            Assert.That(route, Is.Null);
            Assert.That(solver.Calls.Count, Is.EqualTo(1), "쥐는 자리를 못 풀면 나머지는 풀어도 소용없다");
        }

        [Test]
        public void WhenTheNearCannotBeSolved_NoRoute()
        {
            FakeSolver solver = SolvingEverything();
            solver.Solutions.Remove(Near.position);

            Assert.That(Build(solver, out TaughtJointRoute route), Is.False);
            Assert.That(route, Is.Null);
        }

        [Test]
        public void WhenTheRetreatCannotBeSolved_NoRoute()
        {
            FakeSolver solver = SolvingEverything();
            solver.Solutions.Remove(Retreat.position);

            Assert.That(Build(solver, out TaughtJointRoute route), Is.False);
            Assert.That(route, Is.Null);
        }

        [Test]
        public void NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() =>
                TaughtJointRoute.TryBuild(null, Near, Grasp, Retreat, SolvingEverything().Solve, out _));
            Assert.Throws<ArgumentNullException>(() =>
                TaughtJointRoute.TryBuild(Taught, Near, Grasp, Retreat, null, out _));
        }
    }
}
