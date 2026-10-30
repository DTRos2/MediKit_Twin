using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class GraspOrientationSearcherTests
    {
        private static readonly Quaternion Nominal = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        private static readonly Vector3[] Positions = { new Vector3(0f, 1f, 0.2f), new Vector3(0f, 1f, 0.4f) };

        // 도구 끝이 아래로 minDownDeg 이상 기울어야만 닿는 팔을 흉내 낸다.
        private static Func<Pose, bool> NeedsTipDownAtLeast(float minDownDeg, List<Pose> asked = null)
        {
            return pose =>
            {
                asked?.Add(pose);
                Vector3 tip = pose.rotation * Vector3.forward;
                float downDeg = Mathf.Asin(Mathf.Clamp(-tip.y, -1f, 1f)) * Mathf.Rad2Deg;
                return downDeg >= minDownDeg - 1e-3f;
            };
        }

        private static OrientationSearchResult Search(Func<Pose, bool> canReach, OrientationSearchSettings settings)
        {
            var searcher = new GraspOrientationSearcher(canReach);
            var result = new OrientationSearchResult();
            CoroutineRunner.Drain(searcher.Search(Nominal, Positions, settings, result), null);
            return result;
        }

        private static OrientationSearchSettings TiltOnly(float maxDown) =>
            new OrientationSearchSettings(maxDown, 0f, 15f, 0f, 0f, 0f, 0f);

        [Test]
        public void WhenTheNominalReaches_UsesItWithoutSearchingFurther()
        {
            var asked = new List<Pose>();

            OrientationSearchResult result = Search(NeedsTipDownAtLeast(0f, asked), TiltOnly(45f));

            Assert.That(result.IsFound, Is.True);
            Assert.That(result.DeviationDeg, Is.LessThan(1e-2f));
            Assert.That(result.TriedCount, Is.EqualTo(1));
            Assert.That(asked.Count, Is.EqualTo(Positions.Length), "첫 후보를 모든 위치에서 확인하고 멈춘다");
        }

        [Test]
        public void WhenTheNominalDoesNotReach_ChoosesTheSmallestTiltThatDoes()
        {
            OrientationSearchResult result = Search(NeedsTipDownAtLeast(30f), TiltOnly(45f));

            Assert.That(result.IsFound, Is.True);
            Assert.That(result.DeviationDeg, Is.EqualTo(30f).Within(0.5f), "15도로는 모자라고 30도부터 닿는다");
            Assert.That(result.TriedCount, Is.EqualTo(3), "0도, 15도, 30도 순서로 세 번째");
        }

        [Test]
        public void ARotationMustReachEveryPosition_NotJustTheFirst()
        {
            // 첫 위치는 어떤 방향이든 닿지만 두 번째 위치는 30도 이상 기울여야 닿는다.
            Func<Pose, bool> canReach = pose =>
                pose.position == Positions[0] || NeedsTipDownAtLeast(30f)(pose);

            OrientationSearchResult result = Search(canReach, TiltOnly(45f));

            Assert.That(result.DeviationDeg, Is.EqualTo(30f).Within(0.5f));
        }

        [Test]
        public void WhenNothingReaches_ReportsNotFoundWithTheNumberTried()
        {
            OrientationSearchResult result = Search(NeedsTipDownAtLeast(80f), TiltOnly(45f));

            Assert.That(result.IsFound, Is.False);
            Assert.That(result.TriedCount, Is.EqualTo(4), "0, 15, 30, 45도를 모두 확인했다");
        }

        [Test]
        public void ChecksOneCandidatePerFrame()
        {
            var searcher = new GraspOrientationSearcher(NeedsTipDownAtLeast(80f));
            var result = new OrientationSearchResult();
            int frames = 0;

            CoroutineRunner.Drain(searcher.Search(Nominal, Positions, TiltOnly(45f), result), () => frames++);

            Assert.That(frames, Is.GreaterThanOrEqualTo(4), "후보마다 한 프레임씩 양보해 화면이 멈추지 않게 한다");
        }

        [Test]
        public void Constructor_NullProbe_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GraspOrientationSearcher(null));
        }
    }
}
