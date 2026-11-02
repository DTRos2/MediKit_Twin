using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class StandPlacementSearcherTests
    {
        private static readonly Quaternion Nominal = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        private static readonly Vector3[] Positions = { new Vector3(0f, 1f, 0.3f) };

        // 도구 끝이 아래로 0, 15, 30, 45도 기울인 방향만 후보로 삼는다.
        private static readonly OrientationSearchSettings Tilts = new OrientationSearchSettings(45f, 0f, 15f, 0f, 0f, 0f, 0f);

        private static StandCandidate Stand(float extra, float lateral = 0f) => new StandCandidate(new Vector3(lateral, 0f, extra), extra, lateral);

        // 도구 끝이 아래로 minDownDeg 이상 기울어야만 닿는 팔을 흉내 낸다.
        private static Func<Pose, bool> NeedsTipDownAtLeast(float minDownDeg)
        {
            return pose =>
            {
                Vector3 tip = pose.rotation * Vector3.forward;
                float downDeg = Mathf.Asin(Mathf.Clamp(-tip.y, -1f, 1f)) * Mathf.Rad2Deg;
                return downDeg >= minDownDeg - 1e-3f;
            };
        }

        // 뒤로 물러날수록 덜 기울여도 닿는 팔: 기본 거리는 30도, 0.1m 물러나면 15도, 0.2m 물러나면 그대로 닿는다.
        private static Func<StandCandidate, Func<Pose, bool>> ArmThatReachesBetterFurtherBack()
        {
            return candidate => NeedsTipDownAtLeast(Mathf.Max(0f, 30f - candidate.StandoffExtraM * 150f));
        }

        private static StandPlacementResult Search(
            Func<StandCandidate, Func<Pose, bool>> reachabilityAt, StandPlacementCost cost, params StandCandidate[] candidates)
        {
            var searcher = new StandPlacementSearcher(reachabilityAt, cost);
            var result = new StandPlacementResult();
            CoroutineRunner.Drain(searcher.Search(candidates, Nominal, Positions, Tilts, result), null);
            return result;
        }

        // 단계(프레임을 쉬는 것과 안쪽 코루틴으로 들어가는 것)를 센다. 닿는 방향이 없는 팔이 후보 하나를 따지는 데 3ms가 걸린다고 흉내 낸다.
        private static int CountSteps(bool withBudget, out StandPlacementResult result)
        {
            double now = 0d;
            FrameBudget budget = withBudget ? new FrameBudget(0.008, () => now) : null;
            var searcher = new StandPlacementSearcher(
                candidate => pose =>
                {
                    now += 0.003;
                    return false;
                },
                new StandPlacementCost(1f, 100f),
                budget: budget);
            result = new StandPlacementResult();

            int steps = 0;
            CoroutineRunner.Drain(
                searcher.Search(new[] { Stand(0f), Stand(0.1f), Stand(0.2f) }, Nominal, Positions, Tilts, result), () => steps++);
            return steps;
        }

        [Test]
        public void TheBudget_IsSharedAcrossStands_SoFewerFramesAreSpent()
        {
            int withoutBudget = CountSteps(false, out StandPlacementResult plain);
            int withBudget = CountSteps(true, out StandPlacementResult budgeted);

            Assert.That(withBudget, Is.LessThan(withoutBudget));
            Assert.That(budgeted.EvaluatedCount, Is.EqualTo(plain.EvaluatedCount), "예산은 어느 자리를 보는지 바꾸지 않는다");
            Assert.That(budgeted.Options.Count, Is.EqualTo(plain.Options.Count));
        }

        [Test]
        public void TheBestScoringStandComesFirst_NotTheClosestOne()
        {
            // 점수: 기본 자리 30도 → 30, 0.1m → 15 + 10 = 25, 0.2m → 0 + 20 = 20.
            StandPlacementResult result = Search(
                ArmThatReachesBetterFurtherBack(), new StandPlacementCost(1f, 100f), Stand(0f), Stand(0.1f), Stand(0.2f));

            Assert.That(result.Options.Count, Is.EqualTo(3));
            Assert.That(result.Options[0].Candidate.StandoffExtraM, Is.EqualTo(0.2f));
            Assert.That(result.Options[1].Candidate.StandoffExtraM, Is.EqualTo(0.1f));
            Assert.That(result.Options[2].Candidate.StandoffExtraM, Is.EqualTo(0f));
            Assert.That(result.Options[0].Cost, Is.EqualTo(20f).Within(1e-3f));
        }

        [Test]
        public void EachOptionKeepsTheDeviationItReachedWith()
        {
            StandPlacementResult result = Search(
                ArmThatReachesBetterFurtherBack(), new StandPlacementCost(1f, 100f), Stand(0f), Stand(0.1f), Stand(0.2f));

            Assert.That(result.Options[0].DeviationDeg, Is.EqualTo(0f).Within(0.5f));
            Assert.That(result.Options[1].DeviationDeg, Is.EqualTo(15f).Within(0.5f));
            Assert.That(result.Options[2].DeviationDeg, Is.EqualTo(30f).Within(0.5f));
        }

        [Test]
        public void WhenTheBasicStandReachesTheNominal_StopsWithoutTryingFurtherStands()
        {
            var asked = new List<StandCandidate>();
            Func<StandCandidate, Func<Pose, bool>> alwaysReaches = candidate =>
            {
                asked.Add(candidate);
                return NeedsTipDownAtLeast(0f);
            };

            StandPlacementResult result = Search(alwaysReaches, new StandPlacementCost(1f, 100f), Stand(0f), Stand(0.1f), Stand(0.2f));

            Assert.That(result.Options.Count, Is.EqualTo(1));
            Assert.That(asked.Count, Is.EqualTo(1), "뒤 후보는 더 멀리 서므로 이길 수 없다");
            Assert.That(result.EvaluatedCount, Is.EqualTo(1));
        }

        [Test]
        public void AStandThatCannotBeatTheBestEvenWithoutTwistingIsSkipped()
        {
            // 자리를 옮기는 값이 커서 0.1m만 옮겨도 기본 자리에서 30도 비튼 점수(30)를 넘는다.
            StandPlacementResult result = Search(
                ArmThatReachesBetterFurtherBack(), new StandPlacementCost(1f, 1000f), Stand(0f), Stand(0.1f), Stand(0.2f));

            Assert.That(result.Options.Count, Is.EqualTo(1));
            Assert.That(result.Options[0].Candidate.StandoffExtraM, Is.EqualTo(0f));
            Assert.That(result.EvaluatedCount, Is.EqualTo(1));
        }

        [Test]
        public void StandsThatCannotReachAreLeftOut()
        {
            Func<StandCandidate, Func<Pose, bool>> onlyFurtherBackReaches =
                candidate => candidate.StandoffExtraM >= 0.1f ? NeedsTipDownAtLeast(0f) : NeedsTipDownAtLeast(80f);

            StandPlacementResult result = Search(onlyFurtherBackReaches, new StandPlacementCost(1f, 100f), Stand(0f), Stand(0.1f), Stand(0.2f));

            Assert.That(result.Options.Count, Is.EqualTo(1));
            Assert.That(result.Options[0].Candidate.StandoffExtraM, Is.EqualTo(0.1f));
            Assert.That(result.EvaluatedCount, Is.EqualTo(2), "기본 자리는 닿지 않아 다음 후보를 보고, 0.1m에서 방향을 비틀지 않아도 닿아 멈춘다");
        }

        [Test]
        public void WhenNoStandReaches_HasNoOptionsAndCountsEveryStandTried()
        {
            StandPlacementResult result = Search(
                candidate => NeedsTipDownAtLeast(80f), new StandPlacementCost(1f, 100f), Stand(0f), Stand(0.1f), Stand(0.2f));

            Assert.That(result.HasOptions, Is.False);
            Assert.That(result.EvaluatedCount, Is.EqualTo(3));
        }

        [Test]
        public void WhenTheScoresTie_TheStandCloserToTheBasicOneStaysFirst()
        {
            // 두 자리 모두 방향을 15도 비틀어야 닿고, 자리를 옮기는 값이 0이라 점수가 같다.
            StandPlacementResult result = Search(
                candidate => NeedsTipDownAtLeast(15f), new StandPlacementCost(1f, 0f), Stand(0f), Stand(0.1f));

            Assert.That(result.Options.Count, Is.EqualTo(2));
            Assert.That(result.Options[0].Candidate.StandoffExtraM, Is.EqualTo(0f));
        }

        [Test]
        public void EachStandIsAskedAboutItsOwnReach()
        {
            var asked = new List<float>();
            Func<StandCandidate, Func<Pose, bool>> record = candidate =>
            {
                asked.Add(candidate.LateralM);
                return NeedsTipDownAtLeast(80f);
            };

            Search(record, new StandPlacementCost(1f, 100f), Stand(0f, -0.1f), Stand(0f, 0.1f));

            Assert.That(asked, Is.EqualTo(new[] { -0.1f, 0.1f }));
        }

        [Test]
        public void Constructor_NullArgument_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new StandPlacementSearcher(null, new StandPlacementCost(1f, 1f)));
            Assert.Throws<ArgumentNullException>(() => new StandPlacementSearcher(candidate => NeedsTipDownAtLeast(0f), null));
        }
    }
}
