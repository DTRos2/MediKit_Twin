using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class BaseStandClearanceTests
    {
        private sealed class FakeBaseState : IArmBaseStateSource
        {
            internal bool Available = true;

            public bool TryGetState(out Pose armBaseWorldPose, out float rootYawDeg)
            {
                armBaseWorldPose = new Pose(new Vector3(0f, 0.75f, 0f), Quaternion.identity);
                rootYawDeg = 0f;
                return Available;
            }
        }

        private readonly List<string> _log = new List<string>();
        private readonly List<Pose> _asked = new List<Pose>();
        private FakeBaseState _state;

        [SetUp]
        public void SetUp()
        {
            _state = new FakeBaseState();
            _log.Clear();
            _asked.Clear();
        }

        // 서는 자리는 슬롯(0,0,0) 뒤(+z)로 0.55m + 물러난 거리. 슬롯이 옆(x)으로 있으면 그만큼 비킨다.
        private static Vector3? StandBehindSlot(float extra, float lateral) => new Vector3(lateral, 0f, 0.55f + extra);

        // z가 minZ보다 작은 자리에 선 팔 베이스는 레일에 파고든다고 답하는 판정.
        private Func<Pose, string> RailAt(float minZ) => pose =>
        {
            _asked.Add(pose);
            return pose.position.z < minZ ? "Cube" : string.Empty;
        };

        private BaseStandClearance Create(Func<Pose, string> describe, float maxRetreat = 0.15f) =>
            new BaseStandClearance(_state, describe, 0.01f, maxRetreat, _log.Add);

        [Test]
        public void AStandThatDoesNotTouchAnythingIsLeftAlone()
        {
            Func<float, float, Vector3?> standFor = Create(RailAt(0.5f)).Apply(StandBehindSlot, 180f, "박스");

            Vector3? stand = standFor(0f, 0f);

            Assert.That(stand.Value, Is.EqualTo(new Vector3(0f, 0f, 0.55f)));
            Assert.That(_log, Is.Empty);
        }

        [Test]
        public void AStandThatSinksIntoTheRailBacksAwayFromTheSlot()
        {
            Func<float, float, Vector3?> standFor = Create(RailAt(0.585f)).Apply(StandBehindSlot, 180f, "박스");

            Vector3? stand = standFor(0f, 0f);

            // 슬롯에서 멀어지는 쪽(+z)으로 0.55 → 0.59까지 4cm 물러선다.
            Assert.That(stand.Value.z, Is.EqualTo(0.59f).Within(1e-4f));
            Assert.That(stand.Value.x, Is.EqualTo(0f));
        }

        [Test]
        public void TheDirectionComesFromTheStandFunction_NotFromAFixedAxis()
        {
            // 서는 자리가 슬롯의 +x쪽 뒤에 있는 규칙이면, 파고든 자리는 +x쪽으로 물러선다.
            Func<float, float, Vector3?> sideways = (extra, lateral) => new Vector3(0.55f + extra, 0f, lateral);
            Func<Pose, string> railOnX = pose => pose.position.x < 0.575f ? "wall" : string.Empty;

            Vector3? stand = Create(railOnX).Apply(sideways, null, "박스")(0f, 0f);

            Assert.That(stand.Value.x, Is.EqualTo(0.58f).Within(1e-4f));
            Assert.That(stand.Value.z, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void TheExtraDistanceAndLateralOffsetsStillApply()
        {
            Func<float, float, Vector3?> standFor = Create(RailAt(0.5f)).Apply(StandBehindSlot, 180f, "박스");

            Vector3? stand = standFor(0.1f, -0.1f);

            Assert.That(Vector3.Distance(stand.Value, new Vector3(-0.1f, 0f, 0.65f)), Is.LessThan(1e-5f));
        }

        [Test]
        public void WhenNoSpotWithinTheLimitIsFree_TheOriginalStandIsKept_AndItIsSaid()
        {
            Func<float, float, Vector3?> standFor = Create(RailAt(5f), 0.1f).Apply(StandBehindSlot, 180f, "박스");

            Vector3? stand = standFor(0f, 0f);

            Assert.That(stand.Value, Is.EqualTo(new Vector3(0f, 0f, 0.55f)));
            Assert.That(_log.Count, Is.EqualTo(1));
            Assert.That(_log[0], Does.Contain("피할 자리가 없어"));
        }

        [Test]
        public void TheBaseIsAskedAboutAtTheHeightAndTurnItWillHave()
        {
            Create(RailAt(0f)).Apply(StandBehindSlot, 90f, "박스")(0f, 0f);

            // 지금 팔 베이스는 높이 0.75에 있고 루트는 0도다. 90도 돌아선 자리에서의 자세로 물어야 한다.
            Assert.That(_asked.Count, Is.GreaterThan(0));
            Assert.That(_asked[0].position.y, Is.EqualTo(0.75f).Within(1e-5f));
            Assert.That(Quaternion.Angle(_asked[0].rotation, Quaternion.Euler(0f, 90f, 0f)), Is.LessThan(1e-3f));
        }

        [Test]
        public void TheSameAdjustmentIsReportedOnlyOnce_EvenWhenAskedAgain()
        {
            Func<float, float, Vector3?> standFor = Create(RailAt(0.585f)).Apply(StandBehindSlot, 180f, "박스");

            standFor(0f, 0f);
            standFor(0f, 0f);
            standFor(0f, 0f);

            Assert.That(_log.Count, Is.EqualTo(1));
            Assert.That(_log[0], Does.Contain("4cm 물러나"));
            Assert.That(_log[0], Does.Contain("Cube"));
        }

        [Test]
        public void WhenTheBaseStateIsUnknown_TheStandFunctionIsReturnedAsIs()
        {
            _state.Available = false;
            Func<float, float, Vector3?> original = StandBehindSlot;

            Func<float, float, Vector3?> standFor = Create(RailAt(5f)).Apply(original, 180f, "박스");

            Assert.That(standFor, Is.SameAs(original));
        }

        [Test]
        public void ARobotThatCannotMove_HasNoStandToAdjust()
        {
            Func<float, float, Vector3?> fixedArm = (extra, lateral) => null;

            Vector3? stand = Create(RailAt(5f)).Apply(fixedArm, 180f, "박스")(0f, 0f);

            Assert.That(stand.HasValue, Is.False);
        }

        [Test]
        public void WithoutAStandFunction_NothingIsDone()
        {
            Assert.That(Create(RailAt(5f)).Apply(null, 180f, "박스"), Is.Null);
        }
    }
}
