using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class TaughtClearancePlannerTests
    {
        // 도구가 좌우로 돈 각도(도)를 첫 관절 각도(rad)로 삼아 푸는 가짜 풀이. 방향마다 다른 관절 길이 나온다.
        private sealed class YawSolver : ISeededToolSolver
        {
            internal bool Solves = true;

            public bool TrySolveFrom(Pose worldPose, IReadOnlyList<float> seedRad, float[] solutionRad)
            {
                if (!Solves)
                {
                    return false;
                }

                for (int i = 0; i < solutionRad.Length; i++)
                {
                    solutionRad[i] = i == 0 ? Mathf.DeltaAngle(0f, worldPose.rotation.eulerAngles.y) * Mathf.Deg2Rad : 0f;
                }

                return true;
            }
        }

        private static readonly float[] Seed = { 0f, 0f };

        // 좌우로만 10도 간격, 한쪽 20도까지 돌려 본다: 0, ±10, ±20.
        private static readonly OrientationSearchSettings Yaws = new OrientationSearchSettings(0f, 0f, 0f, 20f, 10f, 0f, 0f);

        private readonly YawSolver _solver = new YawSolver();

        // NUnit은 한 시험 클래스 인스턴스를 모든 시험이 함께 쓴다. 풀이를 못 하게 바꾼 시험이 값을 되돌리지 않으면 뒤에 도는 시험이 그 상태를 이어받는다.
        [SetUp]
        public void SetUp()
        {
            _solver.Solves = true;
        }

        private static TaughtPickPlan Plan()
        {
            TaughtPickPose pose = JsonUtility.FromJson<TaughtPickPose>(
                "{\"_slotId\":\"A-1\",\"_toolPosition\":{\"x\":0,\"y\":1,\"z\":0.5},\"_toolRotation\":{\"x\":0,\"y\":0,\"z\":0,\"w\":1},\"_jointDeg\":[0,0]}");
            return TaughtPickPlan.For(pose, Vector3.back, 0.2f, 0.15f, 0.15f);
        }

        private TaughtClearancePlanner Create(Func<GraspTargets, bool> isClear, ClearancePolicy policy = null, Func<GraspTargets, string> describe = null)
        {
            return new TaughtClearancePlanner(new SeededRouteBuilder(_solver), isClear, describe, policy, Yaws);
        }

        private static ClearGraspPlan Run(TaughtClearancePlanner planner)
        {
            var result = new ClearGraspPlan();
            CoroutineRunner.Drain(planner.Plan(Seed, Plan(), result), null);
            return result;
        }

        private static float GraspJointDeg(ClearGraspPlan plan) => plan.Route.Grasp[0] * Mathf.Rad2Deg;

        [Test]
        public void WhenTheTaughtDirectionIsClear_ItIsUsedAsTaught()
        {
            ClearGraspPlan plan = Run(Create(targets => true));

            Assert.That(plan.IsFound, Is.True);
            Assert.That(plan.DeviationDeg, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(plan.TriedCount, Is.EqualTo(1));
            Assert.That(GraspJointDeg(plan), Is.EqualTo(0f).Within(0.5f));
        }

        [Test]
        public void WhenTheTaughtDirectionCollides_ANearbyClearDirectionIsChosen_WithItsOwnJointRoute()
        {
            // 첫 관절이 15도 이상이어야 선반 모서리를 피한다고 본다: 알려 준 방향(0도)과 ±10도는 겹치고 +20도가 처음 겹치지 않는다.
            ClearGraspPlan plan = Run(Create(targets => targets.Route.Grasp[0] * Mathf.Rad2Deg >= 15f));

            Assert.That(plan.IsFound, Is.True);
            Assert.That(plan.HasCollisionRisk, Is.False);
            Assert.That(plan.DeviationDeg, Is.EqualTo(20f).Within(0.5f));
            Assert.That(GraspJointDeg(plan), Is.EqualTo(20f).Within(0.5f), "고른 방향으로 다시 푼 관절 길이다");
        }

        [Test]
        public void TheDirectionTriedIsAskedAboutTogetherWithItsRouteAndPositions()
        {
            var asked = new List<GraspTargets>();
            Run(Create(targets =>
            {
                asked.Add(targets);
                return true;
            }));

            TaughtPickPlan expected = Plan();
            Assert.That(asked.Count, Is.EqualTo(1));
            Assert.That(asked[0].Near, Is.EqualTo(expected.NearPosition));
            Assert.That(asked[0].Grasp, Is.EqualTo(expected.GraspPosition));
            Assert.That(asked[0].Retreat, Is.EqualTo(expected.RetreatPosition));
            Assert.That(asked[0].Route, Is.Not.Null);
        }

        [Test]
        public void WhenEveryDirectionCollides_AndClearIsRequired_NothingIsFound_AndTheFirstCollisionIsKept()
        {
            ClearGraspPlan plan = Run(Create(targets => false, new ClearancePolicy(true, 30), targets => "link_b ↔ shelf_board(경로 40%)"));

            Assert.That(plan.IsFound, Is.False);
            Assert.That(plan.Route, Is.Null);
            Assert.That(plan.TriedCount, Is.EqualTo(5));
            Assert.That(plan.FirstBlock, Does.Contain("shelf_board"));
        }

        [Test]
        public void WhenEveryDirectionCollides_AndRiskIsAllowed_TheTaughtDirectionIsUsedWithARiskMark()
        {
            ClearGraspPlan plan = Run(Create(targets => false, new ClearancePolicy(false, 30), targets => "link_b ↔ shelf_board"));

            Assert.That(plan.IsFound, Is.True);
            Assert.That(plan.HasCollisionRisk, Is.True);
            Assert.That(plan.DeviationDeg, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void WhenTheRouteCannotBeSolved_NothingIsFound()
        {
            _solver.Solves = false;

            Assert.That(Run(Create(targets => true)).IsFound, Is.False);
        }
    }
}
