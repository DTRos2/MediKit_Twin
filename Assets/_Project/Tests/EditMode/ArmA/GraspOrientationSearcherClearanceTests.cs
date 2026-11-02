using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class GraspOrientationSearcherClearanceTests
    {
        private static readonly Quaternion Nominal = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        private static readonly Vector3[] Positions = { new Vector3(0f, 1f, 0.2f), new Vector3(0f, 1f, 0.4f) };

        // 도구 끝이 아래로 0, 15, 30, 45도 기울인 방향만 후보로 삼는다.
        private static readonly OrientationSearchSettings Tilts = new OrientationSearchSettings(45f, 0f, 15f, 0f, 0f, 0f, 0f);

        private static float DownDeg(Pose pose)
        {
            Vector3 tip = pose.rotation * Vector3.forward;
            return Mathf.Asin(Mathf.Clamp(-tip.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        private static Func<Pose, bool> TipDownAtLeast(float minDownDeg) => pose => DownDeg(pose) >= minDownDeg - 1e-3f;

        private static OrientationSearchResult Search(Func<Pose, bool> canReach, Func<Pose, bool> isClear, ClearancePolicy policy = null)
        {
            var searcher = new GraspOrientationSearcher(canReach, isClear, policy);
            var result = new OrientationSearchResult();
            CoroutineRunner.Drain(searcher.Search(Nominal, Positions, Tilts, result), null);
            return result;
        }

        [Test]
        public void WhenTheFirstReachableDirectionIsClear_ItIsChosenWithoutRisk()
        {
            OrientationSearchResult result = Search(pose => true, pose => true);

            Assert.That(result.IsFound, Is.True);
            Assert.That(result.DeviationDeg, Is.LessThan(1e-2f));
            Assert.That(result.HasCollisionRisk, Is.False);
        }

        [Test]
        public void ADirectionThatReachesButCollides_IsSkippedForTheNextClearOne()
        {
            // 모든 방향이 닿지만 도구 끝을 15도 이상 숙여야 겹치지 않는다.
            OrientationSearchResult result = Search(pose => true, TipDownAtLeast(15f));

            Assert.That(result.IsFound, Is.True);
            Assert.That(result.PitchDeg, Is.EqualTo(15f).Within(0.5f));
            Assert.That(result.HasCollisionRisk, Is.False);
        }

        [Test]
        public void ADirectionThatCollides_IsNotChosenEvenIfItWasTheFirstToReach()
        {
            // 닿는 것은 30도 이상, 겹치지 않는 것은 45도뿐: 30도에서 닿지만 겹치므로 45도까지 간다.
            OrientationSearchResult result = Search(TipDownAtLeast(30f), TipDownAtLeast(45f));

            Assert.That(result.IsFound, Is.True);
            Assert.That(result.PitchDeg, Is.EqualTo(45f).Within(0.5f));
            Assert.That(result.HasCollisionRisk, Is.False);
        }

        [Test]
        public void WhenNoReachableDirectionIsClear_TheFirstReachableOneIsUsed_WithARiskMark()
        {
            OrientationSearchResult result = Search(TipDownAtLeast(15f), pose => false);

            Assert.That(result.IsFound, Is.True);
            Assert.That(result.PitchDeg, Is.EqualTo(15f).Within(0.5f), "닿는 방향 중 가장 덜 무리한 방향");
            Assert.That(result.HasCollisionRisk, Is.True);
        }

        [Test]
        public void WhenClearDirectionsAreRequired_NoClearDirectionMeansNotFound()
        {
            OrientationSearchResult result = Search(pose => true, pose => false, new ClearancePolicy(requireClear: true, lookaheadCandidates: 30));

            Assert.That(result.IsFound, Is.False);
            Assert.That(result.HasCollisionRisk, Is.False);
        }

        [Test]
        public void WhenNothingReaches_NothingIsFound_EvenIfEverythingIsClear()
        {
            Assert.That(Search(pose => false, pose => true).IsFound, Is.False);
        }

        [Test]
        public void TheLookaheadLimitsHowFarPastTheFirstReachableDirectionTheSearchGoes()
        {
            var asked = new List<float>();
            Func<Pose, bool> clearOnlyAt45 = pose =>
            {
                float down = DownDeg(pose);
                asked.Add(down);
                return down >= 45f - 1e-3f;
            };

            // 첫 번째로 닿는 방향은 0도. 더 볼 후보 수가 0이면 15도 이후는 보지 않고 0도를 위험 표시로 쓴다.
            OrientationSearchResult noLookahead = Search(pose => true, clearOnlyAt45, new ClearancePolicy(false, 0));
            // 충분히 더 보면 45도(겹치지 않는 방향)를 찾는다.
            OrientationSearchResult wideLookahead = Search(pose => true, clearOnlyAt45, new ClearancePolicy(false, 10));

            Assert.That(noLookahead.HasCollisionRisk, Is.True);
            Assert.That(noLookahead.PitchDeg, Is.EqualTo(0f).Within(0.5f));
            Assert.That(wideLookahead.HasCollisionRisk, Is.False);
            Assert.That(wideLookahead.PitchDeg, Is.EqualTo(45f).Within(0.5f));
        }

        [Test]
        public void WithoutAClearanceQuestion_TheSearchBehavesAsBefore()
        {
            var searcher = new GraspOrientationSearcher(TipDownAtLeast(15f));
            var result = new OrientationSearchResult();

            CoroutineRunner.Drain(searcher.Search(Nominal, Positions, Tilts, result), null);

            Assert.That(result.IsFound, Is.True);
            Assert.That(result.PitchDeg, Is.EqualTo(15f).Within(0.5f));
            Assert.That(result.HasCollisionRisk, Is.False);
        }

        [Test]
        public void ClearanceIsAskedAtEveryPosition_NotOnlyTheFirst()
        {
            var asked = new List<Vector3>();

            Search(pose => true, pose =>
            {
                asked.Add(pose.position);
                return true;
            });

            Assert.That(asked, Is.EquivalentTo(Positions));
        }
    }
}
