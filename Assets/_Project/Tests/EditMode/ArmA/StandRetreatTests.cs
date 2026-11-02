using System;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class StandRetreatTests
    {
        // z가 한계 이상이면 비어 있다고 답하는 판정: 레일이 z=0.5에 있고 그 앞(+z)이 비어 있는 상황.
        private static Func<Vector3, bool> ClearFromZ(float minZ) => stand => stand.z >= minZ - 1e-5f;

        [Test]
        public void WhenTheStandIsAlreadyClear_ItStaysPut()
        {
            var retreat = new StandRetreat(ClearFromZ(0.5f), 0.01f, 0.2f);

            bool found = retreat.TryFind(new Vector3(1f, 0f, 0.6f), Vector3.forward, out Vector3 cleared, out float moved);

            Assert.That(found, Is.True);
            Assert.That(cleared, Is.EqualTo(new Vector3(1f, 0f, 0.6f)));
            Assert.That(moved, Is.EqualTo(0f));
        }

        [Test]
        public void WhenTheStandIsBlocked_ItBacksUpToTheNearestClearSpot()
        {
            var retreat = new StandRetreat(ClearFromZ(0.585f), 0.01f, 0.2f);

            bool found = retreat.TryFind(new Vector3(1f, 0f, 0.553f), Vector3.forward, out Vector3 cleared, out float moved);

            // 0.553에서 3.2cm 이상 물러나야 비므로 한 걸음 1cm 단위로는 4걸음(4cm) 만에 비는 첫 자리다.
            Assert.That(found, Is.True);
            Assert.That(moved, Is.EqualTo(0.04f).Within(1e-4f));
            Assert.That(cleared.z, Is.EqualTo(0.593f).Within(1e-4f));
            Assert.That(cleared.x, Is.EqualTo(1f));
        }

        [Test]
        public void TheRetreatDirectionOnlyUsesItsFloorPart_AndIsNormalized()
        {
            var retreat = new StandRetreat(ClearFromZ(0.52f), 0.01f, 0.2f);

            // 길이가 3이고 위로도 향한 방향이어도 바닥 위 +z로 1cm씩 물러난다.
            bool found = retreat.TryFind(new Vector3(0f, 0f, 0.5f), new Vector3(0f, 2f, 3f), out Vector3 cleared, out float moved);

            Assert.That(found, Is.True);
            Assert.That(cleared.y, Is.EqualTo(0f));
            Assert.That(moved, Is.EqualTo(0.02f).Within(1e-4f));
        }

        [Test]
        public void WhenNoSpotWithinTheLimitIsClear_ItKeepsTheOriginalStand()
        {
            var retreat = new StandRetreat(ClearFromZ(0.9f), 0.01f, 0.1f);

            bool found = retreat.TryFind(new Vector3(0f, 0f, 0.5f), Vector3.forward, out Vector3 cleared, out float moved);

            // 10cm 안에서는 비지 않으므로 엉뚱한 자리로 보내지 않고 원래 자리를 돌려준다.
            Assert.That(found, Is.False);
            Assert.That(cleared, Is.EqualTo(new Vector3(0f, 0f, 0.5f)));
            Assert.That(moved, Is.EqualTo(0f));
        }

        [Test]
        public void TheLimitItselfIsTried()
        {
            var retreat = new StandRetreat(ClearFromZ(0.6f), 0.01f, 0.1f);

            bool found = retreat.TryFind(new Vector3(0f, 0f, 0.5f), Vector3.forward, out _, out float moved);

            Assert.That(found, Is.True);
            Assert.That(moved, Is.EqualTo(0.1f).Within(1e-4f));
        }

        [Test]
        public void WithoutADirection_ABlockedStandCannotBackUp()
        {
            var retreat = new StandRetreat(ClearFromZ(0.6f), 0.01f, 0.2f);

            bool found = retreat.TryFind(new Vector3(0f, 0f, 0.5f), Vector3.zero, out Vector3 cleared, out float moved);

            Assert.That(found, Is.False);
            Assert.That(cleared, Is.EqualTo(new Vector3(0f, 0f, 0.5f)));
            Assert.That(moved, Is.EqualTo(0f));
        }

        [Test]
        public void TheStepMustBePositive()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StandRetreat(ClearFromZ(0f), 0f, 0.1f));
        }
    }
}
