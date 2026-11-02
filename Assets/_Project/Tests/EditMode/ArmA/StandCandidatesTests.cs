using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class StandCandidatesTests
    {
        // 서는 자리를 (옆, 0, 뒤)로 두어 후보가 어느 조합에서 나왔는지 좌표로 알 수 있게 한다.
        private static Vector3 At(float extra, float lateral) => new Vector3(lateral, 0f, extra);

        [Test]
        public void MakesEveryCombinationOfStandoffAndLateral()
        {
            IReadOnlyList<StandCandidate> candidates = StandCandidates.Generate(new[] { 0f, 0.1f, 0.2f }, new[] { 0f, -0.1f, 0.1f }, At);

            Assert.That(candidates.Count, Is.EqualTo(9));
        }

        [Test]
        public void TheStandPointComesFromTheCallerForEachCombination()
        {
            IReadOnlyList<StandCandidate> candidates = StandCandidates.Generate(new[] { 0.2f }, new[] { -0.1f }, At);

            Assert.That(candidates[0].ArmBasePosition, Is.EqualTo(new Vector3(-0.1f, 0f, 0.2f)));
            Assert.That(candidates[0].StandoffExtraM, Is.EqualTo(0.2f));
            Assert.That(candidates[0].LateralM, Is.EqualTo(-0.1f));
        }

        [Test]
        public void CandidatesAreOrderedFromTheClosestToTheBasicStand()
        {
            IReadOnlyList<StandCandidate> candidates = StandCandidates.Generate(new[] { 0.2f, 0f, 0.1f }, new[] { 0.1f, 0f }, At);

            for (int i = 1; i < candidates.Count; i++)
            {
                Assert.That(candidates[i].ShiftM, Is.GreaterThanOrEqualTo(candidates[i - 1].ShiftM));
            }

            Assert.That(candidates[0].ShiftM, Is.EqualTo(0f), "기본 자리가 맨 앞이다");
        }

        [Test]
        public void WhenTheShiftIsTheSame_TheOrderOfTheListsIsKept()
        {
            // (0, -0.1), (0, 0.1), (0.1, 0)은 모두 0.1m 벗어났다. 목록에 적은 순서를 지킨다.
            IReadOnlyList<StandCandidate> candidates = StandCandidates.Generate(new[] { 0f, 0.1f }, new[] { 0f, -0.1f, 0.1f }, At);

            Assert.That(candidates[1].LateralM, Is.EqualTo(-0.1f));
            Assert.That(candidates[2].LateralM, Is.EqualTo(0.1f));
            Assert.That(candidates[3].StandoffExtraM, Is.EqualTo(0.1f));
            Assert.That(candidates[3].LateralM, Is.EqualTo(0f));
        }

        [Test]
        public void TheShiftIsTheStraightDistanceFromTheBasicStand()
        {
            IReadOnlyList<StandCandidate> candidates = StandCandidates.Generate(new[] { 0.3f }, new[] { 0.4f }, At);

            Assert.That(candidates[0].ShiftM, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void ANullArgument_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => StandCandidates.Generate(null, new[] { 0f }, At));
            Assert.Throws<ArgumentNullException>(() => StandCandidates.Generate(new[] { 0f }, null, At));
            Assert.Throws<ArgumentNullException>(() => StandCandidates.Generate(new[] { 0f }, new[] { 0f }, null));
        }
    }
}
