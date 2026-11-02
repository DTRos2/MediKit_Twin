using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class GraspOrientationSearcherBlockerTests
    {
        private static readonly Quaternion Nominal = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        private static readonly Vector3[] Positions = { new Vector3(0f, 1f, 0.2f), new Vector3(0f, 1f, 0.4f), new Vector3(0f, 1.1f, 0.4f) };

        // 도구 끝이 아래로 0, 15, 30, 45도 기울인 방향만 후보로 삼는다.
        private static readonly OrientationSearchSettings Tilts = new OrientationSearchSettings(45f, 0f, 15f, 0f, 0f, 0f, 0f);

        private static OrientationSearchResult Search(
            Func<Quaternion, IReadOnlyList<Vector3>, bool> isClearAlong, Func<Quaternion, IReadOnlyList<Vector3>, string> describeBlock)
        {
            var searcher = new GraspOrientationSearcher(pose => true, null, new ClearancePolicy(true, 30), isClearAlong, null, describeBlock);
            var result = new OrientationSearchResult();
            CoroutineRunner.Drain(searcher.Search(Nominal, Positions, Tilts, result), null);
            return result;
        }

        [Test]
        public void EveryDiscardedDirection_IsExplainedAndCounted()
        {
            // 모든 방향이 닿지만 겹쳐 하나도 쓰지 못한다.
            OrientationSearchResult result = Search((rotation, positions) => false, (rotation, positions) => "link_5 ↔ KitBox/Wall");

            Assert.That(result.IsFound, Is.False);
            Assert.That(result.BlockedCount, Is.EqualTo(4));
            Assert.That(result.Blockers.Describe(4), Is.EqualTo("link_5 ↔ KitBox/Wall 4번"));
        }

        [Test]
        public void TheDirectionThatWasChosen_IsNotExplained()
        {
            var explained = new List<Quaternion>();

            // 처음 두 방향은 겹치고 세 번째부터 겹치지 않는다.
            int asked = 0;
            OrientationSearchResult result = Search(
                (rotation, positions) => ++asked > 2,
                (rotation, positions) =>
                {
                    explained.Add(rotation);
                    return "link_5 ↔ KitBox/Wall";
                });

            Assert.That(result.IsFound, Is.True);
            Assert.That(explained.Count, Is.EqualTo(2), "버려진 두 방향만 설명한다");
            Assert.That(result.Blockers.Describe(4), Does.Contain("2번"));
        }

        [Test]
        public void ADirectionThatCannotBeReached_IsNotExplained()
        {
            var describe = 0;
            var searcher = new GraspOrientationSearcher(
                pose => false, null, new ClearancePolicy(true, 30), (rotation, positions) => false, null,
                (rotation, positions) =>
                {
                    describe++;
                    return "x";
                });
            var result = new OrientationSearchResult();

            CoroutineRunner.Drain(searcher.Search(Nominal, Positions, Tilts, result), null);

            Assert.That(describe, Is.EqualTo(0), "닿지 않는 방향은 겹침을 따지지 않으므로 설명할 것도 없다");
            Assert.That(result.UnreachableCount, Is.EqualTo(4));
            Assert.That(result.Blockers.KindCount, Is.EqualTo(0));
        }

        [Test]
        public void WithoutAnExplainer_NothingIsCounted()
        {
            OrientationSearchResult result = Search((rotation, positions) => false, null);

            Assert.That(result.BlockedCount, Is.EqualTo(4));
            Assert.That(result.Blockers.KindCount, Is.EqualTo(0));
        }

        [Test]
        public void TheVerdictTextNamesTheMostFrequentBlocker()
        {
            OrientationSearchResult result = Search((rotation, positions) => false, (rotation, positions) => "link_5 ↔ KitBox/Wall(경로 30%)");

            Assert.That(result.DescribeVerdicts(), Does.Contain("겹침 4가지"));
            Assert.That(result.DescribeVerdicts(), Does.Contain("link_5 ↔ KitBox/Wall 4번"));
        }

        [Test]
        public void WithoutBlockers_TheVerdictTextHasNoBlockerPart()
        {
            OrientationSearchResult result = Search((rotation, positions) => false, null);

            Assert.That(result.DescribeVerdicts(), Does.Not.Contain("자주 겹친"));
        }
    }
}
