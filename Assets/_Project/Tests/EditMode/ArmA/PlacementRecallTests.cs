using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class PlacementRecallTests
    {
        private static readonly Quaternion Nominal = Quaternion.LookRotation(Vector3.down, Vector3.forward);

        private static void AssertSameRotation(Quaternion actual, Quaternion expected)
        {
            Assert.That(Quaternion.Angle(actual, expected), Is.LessThan(0.01f));
        }

        [Test]
        public void WithNoDeviation_TheToolRotationIsTheNominal()
        {
            var recall = new PlacementRecall(0, true, 0f, 0f, 0f, 0f, 0f);

            AssertSameRotation(recall.ToolRotation(Nominal), Nominal);
        }

        [Test]
        public void TheToolRotation_IsTheSameDirectionTheSearchFound()
        {
            // 방향을 찾은 결과(기울이기, 좌우, 비틀기)만 들고 있어도 같은 방향을 다시 만들어야 기억한 방식이 맞는다.
            OrientationCandidate found = OrientationCandidate.Create(Nominal, 30f, -15f, 90f, OrientationSearchSettings.DefaultRollCostWeight);
            var search = new OrientationSearchResult();
            search.Found(found, 7);

            var recall = new PlacementRecall(2, false, search.PitchDeg, search.YawDeg, search.RollDeg, 0f, 0f);

            AssertSameRotation(recall.ToolRotation(Nominal), found.Rotation);
        }

        [Test]
        public void TheSameDeviation_GivesADifferentRotationFromADifferentNominal()
        {
            // 기본 방향은 쥔 물건마다 달라서, 절대 방향이 아니라 기본 방향에서 벗어난 양을 기억해야 한다.
            var recall = new PlacementRecall(0, true, 30f, 0f, 0f, 0f, 0f);
            Quaternion otherNominal = Quaternion.LookRotation(Vector3.right, Vector3.up);

            Assert.That(Quaternion.Angle(recall.ToolRotation(Nominal), recall.ToolRotation(otherNominal)), Is.GreaterThan(1f));
        }

        [Test]
        public void TheNominalOnlySettings_GiveExactlyOneCandidate_TheNominalItself()
        {
            System.Collections.Generic.IReadOnlyList<Quaternion> candidates =
                OrientationCandidates.Generate(Nominal, OrientationSearchSettings.NominalOnly());

            Assert.That(candidates.Count, Is.EqualTo(1));
            AssertSameRotation(candidates[0], Nominal);
        }

        [Test]
        public void TheStandTheSearchFoundAt_IsRecorded()
        {
            var search = new OrientationSearchResult();
            Assert.That(search.StandoffExtraM, Is.EqualTo(0f));
            Assert.That(search.LateralM, Is.EqualTo(0f));

            search.RecordStand(0.1f, -0.1f);

            Assert.That(search.StandoffExtraM, Is.EqualTo(0.1f));
            Assert.That(search.LateralM, Is.EqualTo(-0.1f));
        }

        [Test]
        public void ANewPlaceRequest_WithoutAnItemKey_HasAnEmptyKey()
        {
            var request = new ItemPlaceRequest(new Pose(Vector3.zero, Quaternion.identity), null);

            Assert.That(request.ItemKey, Is.EqualTo(string.Empty));
            Assert.That(new ItemPlaceRequest(new Pose(Vector3.zero, Quaternion.identity), null, "C-4").ItemKey, Is.EqualTo("C-4"));
        }
    }
}
