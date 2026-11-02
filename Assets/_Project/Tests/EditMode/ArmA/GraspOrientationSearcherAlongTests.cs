using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class GraspOrientationSearcherAlongTests
    {
        private static readonly Quaternion Nominal = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        private static readonly Vector3[] Positions = { new Vector3(0f, 1f, 0.2f), new Vector3(0f, 1f, 0.3f), new Vector3(0f, 1f, 0.4f) };
        private static readonly OrientationSearchSettings Tilts = new OrientationSearchSettings(45f, 0f, 15f, 0f, 0f, 0f, 0f);

        private static float DownDeg(Quaternion rotation)
        {
            Vector3 tip = rotation * Vector3.forward;
            return Mathf.Asin(Mathf.Clamp(-tip.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        private static OrientationSearchResult Search(
            Func<Pose, bool> isClear, Func<Quaternion, IReadOnlyList<Vector3>, bool> along, ClearancePolicy policy = null)
        {
            var searcher = new GraspOrientationSearcher(pose => true, isClear, policy, along);
            var result = new OrientationSearchResult();
            CoroutineRunner.Drain(searcher.Search(Nominal, Positions, Tilts, result), null);
            return result;
        }

        [Test]
        public void WhenTheRouteQuestionIsGiven_ItReplacesThePerPoseQuestion()
        {
            // 자세마다 묻는 쪽은 항상 겹친다고 하지만, 길 전체를 묻는 쪽이 괜찮다고 하면 그 방향을 쓴다.
            OrientationSearchResult result = Search(pose => false, (rotation, positions) => true);

            Assert.That(result.IsFound, Is.True);
            Assert.That(result.HasCollisionRisk, Is.False);
            Assert.That(result.DeviationDeg, Is.LessThan(1e-2f));
        }

        [Test]
        public void ADirectionWhoseRouteCollides_IsSkipped_EvenIfEveryPoseAlongItIsClear()
        {
            // 각 자세는 겹치지 않지만, 도구 끝을 30도 이상 숙여야 길이 선반 모서리를 피한다.
            OrientationSearchResult result = Search(pose => true, (rotation, positions) => DownDeg(rotation) >= 30f - 1e-3f);

            Assert.That(result.IsFound, Is.True);
            Assert.That(result.PitchDeg, Is.EqualTo(30f).Within(0.5f));
        }

        [Test]
        public void TheRouteQuestionReceivesAllThePositionsInOrder()
        {
            IReadOnlyList<Vector3> received = null;
            Search(pose => true, (rotation, positions) =>
            {
                received = positions;
                return true;
            });

            Assert.That(received, Is.EqualTo(Positions));
        }

        [Test]
        public void WhenNoDirectionHasAClearRoute_AndClearIsRequired_NothingIsFound()
        {
            OrientationSearchResult result = Search(pose => true, (rotation, positions) => false, new ClearancePolicy(true, 30));

            Assert.That(result.IsFound, Is.False);
        }
    }
}
