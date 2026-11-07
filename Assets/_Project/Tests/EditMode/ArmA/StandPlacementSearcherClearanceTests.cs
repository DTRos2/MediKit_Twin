using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class StandPlacementSearcherClearanceTests
    {
        private static readonly Quaternion Nominal = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        private static readonly Vector3[] Positions = { new Vector3(0f, 1f, 0.3f) };
        private static readonly OrientationSearchSettings Tilts = new OrientationSearchSettings(45f, 0f, 15f, 0f, 0f, 0f, 0f);

        private static StandCandidate Stand(float extra) => new StandCandidate(new Vector3(0f, 0f, extra), extra, 0f);

        private static StandPlacementResult Search(
            Func<StandCandidate, Func<Pose, bool>> clearanceAt, ClearancePolicy policy, params StandCandidate[] candidates)
        {
            var searcher = new StandPlacementSearcher(candidate => pose => true, new StandPlacementCost(1f, 100f), clearanceAt, policy);
            var result = new StandPlacementResult();
            CoroutineRunner.Drain(searcher.Search(candidates, Nominal, Positions, Tilts, result), null);
            return result;
        }

        // 기본 자리(뒤로 0m)에서는 어느 방향이든 겹치고, 더 물러난 자리에서는 겹치지 않는다.
        private static Func<StandCandidate, Func<Pose, bool>> ClearOnlyWhenFurtherBack(float minExtra)
        {
            return candidate => pose => candidate.StandoffExtraM >= minExtra;
        }

        [Test]
        public void AStandWithAClearDirection_BeatsACloserStandThatOnlyHasRiskyOnes()
        {
            StandPlacementResult result = Search(ClearOnlyWhenFurtherBack(0.2f), ClearancePolicy.Default, Stand(0f), Stand(0.2f));

            Assert.That(result.Options.Count, Is.EqualTo(2));
            Assert.That(result.Options[0].Candidate.StandoffExtraM, Is.EqualTo(0.2f));
            Assert.That(result.Options[0].HasCollisionRisk, Is.False);
            Assert.That(result.Options[1].Candidate.StandoffExtraM, Is.EqualTo(0f));
            Assert.That(result.Options[1].HasCollisionRisk, Is.True);
            Assert.That(result.Options[1].Cost, Is.GreaterThan(result.Options[0].Cost + 500f), "겹침을 감수하는 자리는 큰 벌점을 받는다");
        }

        [Test]
        public void WhenClearDirectionsAreRequired_OnlyTheClearStandsRemain()
        {
            StandPlacementResult result = Search(
                ClearOnlyWhenFurtherBack(0.2f), new ClearancePolicy(requireClear: true, lookaheadCandidates: 30), Stand(0f), Stand(0.2f));

            Assert.That(result.Options.Count, Is.EqualTo(1));
            Assert.That(result.Options[0].Candidate.StandoffExtraM, Is.EqualTo(0.2f));
        }

        [Test]
        public void WhenTheBasicStandIsAlreadyClear_FurtherStandsAreNotTried()
        {
            int asked = 0;
            Func<StandCandidate, Func<Pose, bool>> clearAnywhere = candidate => pose =>
            {
                asked++;
                return true;
            };

            StandPlacementResult result = Search(clearAnywhere, ClearancePolicy.Default, Stand(0f), Stand(0.1f), Stand(0.2f));

            Assert.That(result.Options.Count, Is.EqualTo(1));
            Assert.That(result.Options[0].HasCollisionRisk, Is.False);
            Assert.That(result.EvaluatedCount, Is.EqualTo(1));
            Assert.That(asked, Is.GreaterThan(0));
        }

        [Test]
        public void WithoutAClearanceQuestion_NoOptionCarriesARisk()
        {
            StandPlacementResult result = Search(null, ClearancePolicy.Default, Stand(0f), Stand(0.2f));

            foreach (StandPlacementOption option in result.Options)
            {
                Assert.That(option.HasCollisionRisk, Is.False);
            }
        }

        [Test]
        public void ThePlanningQuestionForAStand_IsBuiltForThatStandOnly()
        {
            var asked = new System.Collections.Generic.List<float>();
            Func<StandCandidate, Func<Pose, bool>> record = candidate => pose =>
            {
                asked.Add(candidate.StandoffExtraM);
                return true;
            };

            Search(record, ClearancePolicy.Default, Stand(0f));

            Assert.That(asked, Is.All.EqualTo(0f));
        }
    }
}
