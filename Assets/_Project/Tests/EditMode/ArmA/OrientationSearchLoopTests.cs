using System;
using System.Collections;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class OrientationSearchLoopTests
    {
        private static readonly Quaternion Nominal = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        // 도구 끝이 아래로 0, 15, 30, 45도 기울인 방향만 후보로 삼는다(기본 방향에서 가까운 순서).
        private static readonly OrientationSearchSettings Tilts = new OrientationSearchSettings(45f, 0f, 15f, 0f, 0f, 0f, 0f);

        private static float DownDeg(Quaternion rotation)
        {
            Vector3 tip = rotation * Vector3.forward;
            return Mathf.Asin(Mathf.Clamp(-tip.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        private static Func<Quaternion, CandidateVerdict> ByDownDeg(float reachesAt, float clearAt) => rotation =>
        {
            float down = DownDeg(rotation);
            if (down < reachesAt - 1e-3f)
            {
                return CandidateVerdict.Unreachable;
            }

            return down < clearAt - 1e-3f ? CandidateVerdict.Blocked : CandidateVerdict.Clear;
        };

        private static OrientationSearchResult Run(Func<Quaternion, CandidateVerdict> evaluate, ClearancePolicy policy = null)
        {
            var result = new OrientationSearchResult();
            CoroutineRunner.Drain(new OrientationSearchLoop(policy).Run(Nominal, Tilts, evaluate, result), null);
            return result;
        }

        [Test]
        public void TheFirstClearCandidate_IsChosen_WhichIsTheNominalWhenItIsClear()
        {
            OrientationSearchResult result = Run(rotation => CandidateVerdict.Clear);

            Assert.That(result.IsFound, Is.True);
            Assert.That(result.DeviationDeg, Is.LessThan(1e-2f));
            Assert.That(result.TriedCount, Is.EqualTo(1));
        }

        [Test]
        public void UnreachableAndBlockedCandidates_AreSkippedForTheFirstClearOne()
        {
            OrientationSearchResult result = Run(ByDownDeg(reachesAt: 15f, clearAt: 30f));

            Assert.That(result.IsFound, Is.True);
            Assert.That(result.PitchDeg, Is.EqualTo(30f).Within(0.5f));
            Assert.That(result.HasCollisionRisk, Is.False);
        }

        [Test]
        public void OneCandidateIsEvaluatedPerFrame()
        {
            var result = new OrientationSearchResult();
            IEnumerator frames = new OrientationSearchLoop(null).Run(Nominal, Tilts, ByDownDeg(0f, 30f), result);

            int waits = 0;
            while (frames.MoveNext())
            {
                waits++;
            }

            Assert.That(waits, Is.EqualTo(2), "세 번째 후보에서 찾으므로 앞의 두 후보 뒤에서 한 프레임씩 쉰다");
            Assert.That(result.TriedCount, Is.EqualTo(3));
        }

        // 후보 하나를 따지는 데 evaluateSeconds가 걸리는 것처럼 시계를 흘리는 평가 함수와, 그 시계를 쓰는 예산.
        private sealed class SlowEvaluation
        {
            private double _now;
            private readonly double _evaluateSeconds;

            internal SlowEvaluation(double evaluateSeconds, double sliceSeconds)
            {
                _evaluateSeconds = evaluateSeconds;
                Budget = new FrameBudget(sliceSeconds, () => _now);
            }

            internal FrameBudget Budget { get; }

            internal CandidateVerdict Blocked(Quaternion rotation)
            {
                _now += _evaluateSeconds;
                return CandidateVerdict.Blocked;
            }
        }

        // 프레임을 쉰 횟수를 센다.
        private static int CountRests(OrientationSearchLoop loop, Func<Quaternion, CandidateVerdict> evaluate, OrientationSearchResult result)
        {
            IEnumerator frames = loop.Run(Nominal, Tilts, evaluate, result);
            int rests = 0;
            while (frames.MoveNext())
            {
                rests++;
            }

            return rests;
        }

        [Test]
        public void WithABudget_SeveralCandidatesAreEvaluatedBeforeRestingAFrame()
        {
            var slow = new SlowEvaluation(evaluateSeconds: 0.003, sliceSeconds: 0.008);
            var result = new OrientationSearchResult();

            int rests = CountRests(new OrientationSearchLoop(new ClearancePolicy(true, 30), slow.Budget), slow.Blocked, result);

            Assert.That(result.TriedCount, Is.EqualTo(4), "끝까지 본다");
            Assert.That(rests, Is.EqualTo(1), "3ms씩 3번째에서 8ms를 넘겨 한 번만 쉰다(예산이 없으면 4번 쉰다)");
        }

        [Test]
        public void WithABudgetThatIsNeverSpent_ItNeverRests()
        {
            var slow = new SlowEvaluation(evaluateSeconds: 0.0, sliceSeconds: 0.008);
            var result = new OrientationSearchResult();

            int rests = CountRests(new OrientationSearchLoop(new ClearancePolicy(true, 30), slow.Budget), slow.Blocked, result);

            Assert.That(rests, Is.EqualTo(0));
            Assert.That(result.TriedCount, Is.EqualTo(4));
        }

        [Test]
        public void WithAZeroBudget_ItRestsAfterEveryCandidate_LikeNoBudget()
        {
            var slow = new SlowEvaluation(evaluateSeconds: 0.001, sliceSeconds: 0.0);

            int withBudget = CountRests(
                new OrientationSearchLoop(new ClearancePolicy(true, 30), slow.Budget), slow.Blocked, new OrientationSearchResult());
            int withoutBudget = CountRests(
                new OrientationSearchLoop(new ClearancePolicy(true, 30)), slow.Blocked, new OrientationSearchResult());

            Assert.That(withBudget, Is.EqualTo(withoutBudget));
            Assert.That(withoutBudget, Is.EqualTo(4));
        }

        [Test]
        public void TheBudget_DoesNotChangeWhichCandidateIsChosen()
        {
            var slow = new SlowEvaluation(evaluateSeconds: 0.003, sliceSeconds: 0.008);
            Func<Quaternion, CandidateVerdict> evaluate = rotation =>
            {
                slow.Blocked(rotation);
                return ByDownDeg(15f, 30f)(rotation);
            };
            var withBudget = new OrientationSearchResult();
            var withoutBudget = new OrientationSearchResult();

            CountRests(new OrientationSearchLoop(null, slow.Budget), evaluate, withBudget);
            CountRests(new OrientationSearchLoop(null), evaluate, withoutBudget);

            Assert.That(withBudget.IsFound, Is.True);
            Assert.That(withBudget.PitchDeg, Is.EqualTo(withoutBudget.PitchDeg).Within(1e-3f));
            Assert.That(withBudget.TriedCount, Is.EqualTo(withoutBudget.TriedCount));
        }

        [Test]
        public void WhenNothingIsClear_AndClearIsRequired_NothingIsFound()
        {
            OrientationSearchResult result = Run(ByDownDeg(0f, 999f), new ClearancePolicy(true, 30));

            Assert.That(result.IsFound, Is.False);
            Assert.That(result.TriedCount, Is.EqualTo(4), "끝까지 본다");
        }

        [Test]
        public void ItCountsHowManyCandidatesWereUnreachableAndHowManyWereBlocked()
        {
            // 기울기 0, 15, 30, 45°를 차례로 본다: 0°는 닿지 않고, 15°와 30°는 닿지만 겹치고, 45°는 겹치지 않는다.
            OrientationSearchResult result = Run(ByDownDeg(reachesAt: 15f, clearAt: 45f));

            Assert.That(result.IsFound, Is.True);
            Assert.That(result.UnreachableCount, Is.EqualTo(1));
            Assert.That(result.BlockedCount, Is.EqualTo(2));
        }

        [Test]
        public void WhenNothingIsFound_TheCountsSayWhetherReachOrObstaclesWereTheReason()
        {
            OrientationSearchResult onlyBlocked = Run(ByDownDeg(0f, 999f), new ClearancePolicy(true, 30));
            OrientationSearchResult onlyUnreachable = Run(rotation => CandidateVerdict.Unreachable, new ClearancePolicy(true, 30));

            Assert.That(onlyBlocked.UnreachableCount, Is.EqualTo(0));
            Assert.That(onlyBlocked.BlockedCount, Is.EqualTo(4));
            Assert.That(onlyUnreachable.UnreachableCount, Is.EqualTo(4));
            Assert.That(onlyUnreachable.BlockedCount, Is.EqualTo(0));
            Assert.That(onlyBlocked.DescribeVerdicts(), Does.Contain("겹침 4"));
        }

        [Test]
        public void WhenNothingIsClear_AndRiskIsAllowed_TheFirstBlockedCandidateIsUsedWithARiskMark()
        {
            OrientationSearchResult result = Run(ByDownDeg(15f, 999f), new ClearancePolicy(false, 30));

            Assert.That(result.IsFound, Is.True);
            Assert.That(result.HasCollisionRisk, Is.True);
            Assert.That(result.PitchDeg, Is.EqualTo(15f).Within(0.5f));
        }

        [Test]
        public void WhenNothingReaches_NothingIsFound_EvenIfRiskIsAllowed()
        {
            OrientationSearchResult result = Run(rotation => CandidateVerdict.Unreachable, new ClearancePolicy(false, 30));

            Assert.That(result.IsFound, Is.False);
        }

        [Test]
        public void WhenRiskIsAllowed_ItStopsLookingAfterTheLookaheadWindow()
        {
            int evaluated = 0;
            Func<Quaternion, CandidateVerdict> neverClear = rotation =>
            {
                evaluated++;
                return CandidateVerdict.Blocked;
            };

            OrientationSearchResult result = Run(neverClear, new ClearancePolicy(false, 1));

            Assert.That(result.IsFound, Is.True);
            Assert.That(evaluated, Is.EqualTo(2), "처음 겹친 후보(0)와 그 뒤 1개(1)까지만 보고 멈춘다");
        }
    }
}
