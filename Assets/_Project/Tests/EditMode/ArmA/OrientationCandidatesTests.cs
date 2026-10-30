using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class OrientationCandidatesTests
    {
        // 수평으로 칸 안쪽(+Z)을 보고 위쪽이 월드 위인 기본 방향.
        private static readonly Quaternion Nominal = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        private static IReadOnlyList<Quaternion> Generate(OrientationSearchSettings settings) =>
            OrientationCandidates.Generate(Nominal, settings);

        [Test]
        public void FirstCandidate_IsTheNominalOrientation()
        {
            IReadOnlyList<Quaternion> candidates = Generate(new OrientationSearchSettings(45f, 15f, 15f, 30f, 15f, 90f, 90f));

            Assert.That(Quaternion.Angle(candidates[0], Nominal), Is.LessThan(1e-3f));
        }

        [Test]
        public void Candidates_AreOrderedByDistanceFromTheNominal()
        {
            IReadOnlyList<Quaternion> candidates = Generate(new OrientationSearchSettings(30f, 30f, 15f, 15f, 15f, 0f, 0f));

            float previous = -1f;
            foreach (Quaternion candidate in candidates)
            {
                float angle = Quaternion.Angle(Nominal, candidate);
                // 정렬 기준(축별 각도의 제곱합 제곱근)과 실제 회전 각도는 근사라서 조금의 오차는 허용한다.
                Assert.That(angle, Is.GreaterThanOrEqualTo(previous - 0.5f));
                previous = angle;
            }
        }

        [Test]
        public void Count_IsTheProductOfTheStepCountsOfEachAxis()
        {
            // 기울이기: 0, +15, +30, -15 = 4, 좌우: 0, +15, -15 = 3, 비틀기: 0 = 1
            IReadOnlyList<Quaternion> candidates = Generate(new OrientationSearchSettings(30f, 15f, 15f, 15f, 15f, 0f, 0f));

            Assert.That(candidates.Count, Is.EqualTo(4 * 3 * 1));
        }

        [Test]
        public void ZeroSteps_GiveOnlyTheNominal()
        {
            IReadOnlyList<Quaternion> candidates = Generate(new OrientationSearchSettings(45f, 15f, 0f, 30f, 0f, 90f, 0f));

            Assert.That(candidates.Count, Is.EqualTo(1));
        }

        [Test]
        public void PositivePitch_TiltsTheToolTipDownward()
        {
            // 기울이기만 켜고 좌우와 비틀기는 끈다. 두 번째 이후 후보 중 아래로 기운 것이 있어야 한다.
            IReadOnlyList<Quaternion> candidates = Generate(new OrientationSearchSettings(30f, 0f, 30f, 0f, 0f, 0f, 0f));

            Assert.That(candidates.Count, Is.EqualTo(2));
            Vector3 tip = candidates[1] * Vector3.forward;
            Assert.That(tip.y, Is.LessThan(-0.4f), "도구 끝이 아래를 향한다");
        }

        [Test]
        public void PitchUpIsLimitedByMaxPitchUp()
        {
            // 위로는 한 칸(15도)도 허용하지 않는다.
            IReadOnlyList<Quaternion> candidates = Generate(new OrientationSearchSettings(45f, 0f, 15f, 0f, 0f, 0f, 0f));

            foreach (Quaternion candidate in candidates)
            {
                Assert.That((candidate * Vector3.forward).y, Is.LessThanOrEqualTo(1e-4f), "도구 끝이 위로 들리면 안 된다");
            }
        }

        [Test]
        public void RollOnly_KeepsTheApproachAxis()
        {
            IReadOnlyList<Quaternion> candidates = Generate(new OrientationSearchSettings(0f, 0f, 0f, 0f, 0f, 90f, 90f));

            Assert.That(candidates.Count, Is.EqualTo(3));
            foreach (Quaternion candidate in candidates)
            {
                Assert.That(Vector3.Angle(candidate * Vector3.forward, Vector3.forward), Is.LessThan(1e-2f));
            }
        }

        [Test]
        public void NullSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => OrientationCandidates.Generate(Nominal, null));
        }
    }
}
