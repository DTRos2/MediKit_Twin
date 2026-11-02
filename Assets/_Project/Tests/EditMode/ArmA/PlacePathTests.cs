using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class PlacePathTests
    {
        private static readonly Vector3 Slot = new Vector3(1.5f, 1f, 0.1f);

        [Test]
        public void WithoutTheHeldItemsPose_TheToolGoesToTheSlotItself()
        {
            PlacePath path = PlacePath.For(null, Slot, Quaternion.identity, 0.15f, 0.2f);

            Assert.That(path.Slot, Is.EqualTo(Slot));
            Assert.That(path.Above, Is.EqualTo(Slot + Vector3.up * 0.15f));
            Assert.That(path.Lift, Is.EqualTo(Slot + Vector3.up * 0.2f));
        }

        [Test]
        public void WithoutAReleaseHeight_TheToolReleasesAtTheSlotItself()
        {
            PlacePath path = PlacePath.For(null, Slot, Quaternion.identity, 0.15f, 0.2f);

            Assert.That(path.Slot, Is.EqualTo(PlacePath.For(null, Slot, Quaternion.identity, 0.15f, 0.2f, 0f).Slot));
        }

        [Test]
        public void AReleaseHeight_LiftsTheWholePathSoTheItemIsLetGoAboveTheSlot()
        {
            PlacePath path = PlacePath.For(null, Slot, Quaternion.identity, 0.15f, 0.2f, 0.11f);

            // float 덧셈 순서가 달라 마지막 비트가 어긋날 수 있어 거리로 견준다.
            Assert.That(Vector3.Distance(path.Slot, Slot + Vector3.up * 0.11f), Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(path.Above, Slot + Vector3.up * (0.11f + 0.15f)), Is.LessThan(1e-5f), "접근은 놓는 높이에서 다시 위로 잰다");
            Assert.That(Vector3.Distance(path.Lift, Slot + Vector3.up * (0.11f + 0.2f)), Is.LessThan(1e-5f), "후퇴도 놓는 높이에서 위로 잰다");
        }

        [Test]
        public void AReleaseHeight_KeepsTheItemCenterOnTheVerticalLineThroughTheSlot()
        {
            HeldItemFrame frame = HeldItemFrame.Measure(new Pose(Vector3.zero, Quaternion.identity), new Vector3(0f, 0f, 0.03f), Quaternion.identity);
            Quaternion rotation = Quaternion.Euler(40f, 25f, 0f);

            PlacePath path = PlacePath.For(frame, Slot, rotation, 0.15f, 0.2f, 0.11f);

            Vector3 itemCenterAtRelease = path.Slot + rotation * frame.CenterInTool;
            Assert.That(Vector3.Distance(itemCenterAtRelease, Slot + Vector3.up * 0.11f), Is.LessThan(1e-5f), "떨어지는 물건이 슬롯 바로 위에서 시작한다");
        }

        [Test]
        public void WithTheHeldItemsPose_TheItemCenterPassesThroughTheSlotAboveAndLiftSpots()
        {
            // 물건이 도구 앞 3cm에 붙어 있으면, 도구가 어떤 방향이든 도구 자리에서 그만큼 앞에 물건 중심이 와야 한다.
            HeldItemFrame frame = HeldItemFrame.Measure(new Pose(Vector3.zero, Quaternion.identity), new Vector3(0f, 0f, 0.03f), Quaternion.identity);
            Quaternion rotation = Quaternion.Euler(40f, 25f, 0f);

            PlacePath path = PlacePath.For(frame, Slot, rotation, 0.15f, 0.2f);

            Vector3 offset = rotation * frame.CenterInTool;
            Assert.That(Vector3.Distance(path.Slot + offset, Slot), Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(path.Above + offset, Slot + Vector3.up * 0.15f), Is.LessThan(1e-5f));
            Assert.That(Vector3.Distance(path.Lift + offset, Slot + Vector3.up * 0.2f), Is.LessThan(1e-5f));
        }
    }
}
