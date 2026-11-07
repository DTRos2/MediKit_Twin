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

        private static IReadOnlyList<OrientationCandidate> Ranked(float rollCostWeight) =>
            OrientationCandidates.GenerateRanked(Nominal, new OrientationSearchSettings(45f, 15f, 15f, 30f, 15f, 90f, 90f, rollCostWeight));

        [Test]
        public void DefaultRollCostWeight_CountsRollAsATenthOfTilt()
        {
            var settings = new OrientationSearchSettings(45f, 15f, 15f, 30f, 15f, 90f, 90f);

            Assert.That(settings.RollCostWeight, Is.EqualTo(0.1f).Within(1e-4f));
        }

        [Test]
        public void ARollOnlyCandidate_IsCountedByTheRollWeight()
        {
            OrientationCandidate rollOnly = FindCandidate(Ranked(0.1f), pitch: 0f, yaw: 0f, roll: 90f);

            Assert.That(rollOnly.DeviationDeg, Is.EqualTo(9f).Within(1e-3f), "90도를 0.1배로 센다");
        }

        [Test]
        public void WithTheDefaultWeight_RollingTheWristComesBeforeTilting()
        {
            IReadOnlyList<OrientationCandidate> ranked = Ranked(0.1f);

            Assert.That(ranked[0].DeviationDeg, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(Mathf.Abs(ranked[1].RollDeg), Is.EqualTo(90f), "기본 다음은 손목만 비튼 후보(9)이고 기울이기 15도보다 앞이다");
            Assert.That(ranked[1].PitchDeg, Is.EqualTo(0f));
            Assert.That(ranked[1].YawDeg, Is.EqualTo(0f));
        }

        [Test]
        public void WithWeightOne_RollIsCountedLikeTilt()
        {
            OrientationCandidate rollOnly = FindCandidate(Ranked(1f), pitch: 0f, yaw: 0f, roll: 90f);

            Assert.That(rollOnly.DeviationDeg, Is.EqualTo(90f).Within(1e-3f));
        }

        [Test]
        public void WithWeightZero_RollIsFree()
        {
            OrientationCandidate rollOnly = FindCandidate(Ranked(0f), pitch: 0f, yaw: 0f, roll: 90f);

            Assert.That(rollOnly.DeviationDeg, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void TiltAndYaw_AreNotReducedByTheRollWeight()
        {
            OrientationCandidate tilt = FindCandidate(Ranked(0.1f), pitch: 30f, yaw: 0f, roll: 0f);
            OrientationCandidate yaw = FindCandidate(Ranked(0.1f), pitch: 0f, yaw: 30f, roll: 0f);

            Assert.That(tilt.DeviationDeg, Is.EqualTo(30f).Within(1e-3f));
            Assert.That(yaw.DeviationDeg, Is.EqualTo(30f).Within(1e-3f));
        }

        [Test]
        public void AMixedCandidate_CombinesTheAxesBySumOfSquares()
        {
            OrientationCandidate mixed = FindCandidate(Ranked(0.1f), pitch: 30f, yaw: 0f, roll: 90f);

            Assert.That(mixed.DeviationDeg, Is.EqualTo(Mathf.Sqrt(30f * 30f + 9f * 9f)).Within(1e-3f));
        }

        [Test]
        public void EachCandidateKeepsTheAnglesItWasMadeWith()
        {
            IReadOnlyList<OrientationCandidate> ranked =
                OrientationCandidates.GenerateRanked(Nominal, new OrientationSearchSettings(30f, 0f, 30f, 0f, 0f, 0f, 0f));

            Assert.That(ranked.Count, Is.EqualTo(2));
            Assert.That(ranked[1].PitchDeg, Is.EqualTo(30f));
            Assert.That(ranked[1].YawDeg, Is.EqualTo(0f));
            Assert.That(ranked[1].RollDeg, Is.EqualTo(0f));
        }

        [Test]
        public void ARolledCandidate_TurnsTheToolAroundItsApproachAxisByTheRollAngle()
        {
            OrientationCandidate rollOnly = FindCandidate(Ranked(0.1f), pitch: 0f, yaw: 0f, roll: 90f);

            Assert.That(Vector3.Angle(rollOnly.Rotation * Vector3.forward, Vector3.forward), Is.LessThan(1e-2f));
            Assert.That(Quaternion.Angle(Nominal, rollOnly.Rotation), Is.EqualTo(90f).Within(0.1f), "실제 회전은 90도지만 점수에서는 9도로 센다");
        }

        [Test]
        public void GenerateRanked_NullSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => OrientationCandidates.GenerateRanked(Nominal, null));
        }

        private static OrientationCandidate FindCandidate(IReadOnlyList<OrientationCandidate> ranked, float pitch, float yaw, float roll)
        {
            foreach (OrientationCandidate candidate in ranked)
            {
                if (Mathf.Approximately(candidate.PitchDeg, pitch)
                    && Mathf.Approximately(candidate.YawDeg, yaw)
                    && Mathf.Approximately(candidate.RollDeg, roll))
                {
                    return candidate;
                }
            }

            Assert.Fail($"기울이기 {pitch}, 좌우 {yaw}, 비틀기 {roll} 후보가 없다.");
            return default;
        }
    }
}
