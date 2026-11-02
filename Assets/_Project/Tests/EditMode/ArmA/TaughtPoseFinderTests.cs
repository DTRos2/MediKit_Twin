using System;
using System.IO;
using NUnit.Framework;

namespace MediKitTwin.ArmA.Tests
{
    public sealed class TaughtPoseFinderTests
    {
        private sealed class FakeSource : IPickPoseSource
        {
            internal TaughtPickPose Pose;
            internal Exception ToThrow;

            public bool TryGet(string slotId, out TaughtPickPose pose)
            {
                if (ToThrow != null)
                {
                    throw ToThrow;
                }

                pose = Pose;
                return Pose != null;
            }
        }

        [Test]
        public void FindsThePose_WhenTheSourceHasIt()
        {
            var taught = new TaughtPickPose();
            var finder = new TaughtPoseFinder(new FakeSource { Pose = taught }, null);

            bool found = finder.TryFind("A-1", out TaughtPickPose pose);

            Assert.That(found, Is.True);
            Assert.That(pose, Is.SameAs(taught));
        }

        [Test]
        public void FindsNothing_WhenTheSourceHasNoPoseForTheSlot()
        {
            var finder = new TaughtPoseFinder(new FakeSource(), null);

            Assert.That(finder.TryFind("A-1", out TaughtPickPose pose), Is.False);
            Assert.That(pose, Is.Null);
        }

        [Test]
        public void FindsNothing_WhenTaughtPosesAreOff_OrTheSlotIdIsEmpty()
        {
            var off = new TaughtPoseFinder(null, null);
            var noSlot = new TaughtPoseFinder(new FakeSource { Pose = new TaughtPickPose() }, null);

            Assert.That(off.TryFind("A-1", out _), Is.False);
            Assert.That(noSlot.TryFind(string.Empty, out _), Is.False);
            Assert.That(noSlot.TryFind(null, out _), Is.False);
        }

        [TestCase(typeof(IOException))]
        [TestCase(typeof(ArgumentException))]
        public void TreatsAnUnreadableFileAsNoPose_AndReportsIt(Type exceptionType)
        {
            string reported = null;
            var source = new FakeSource { ToThrow = (Exception)Activator.CreateInstance(exceptionType, "깨진 파일") };
            var finder = new TaughtPoseFinder(source, message => reported = message);

            bool found = finder.TryFind("A-1", out TaughtPickPose pose);

            Assert.That(found, Is.False);
            Assert.That(pose, Is.Null);
            Assert.That(reported, Does.Contain("깨진 파일"));
        }

        [Test]
        public void DoesNotSwallowUnexpectedErrors()
        {
            var finder = new TaughtPoseFinder(new FakeSource { ToThrow = new InvalidOperationException() }, null);

            Assert.Throws<InvalidOperationException>(() => finder.TryFind("A-1", out _));
        }

        [Test]
        public void ExplainsWhyThereIsNoPose()
        {
            var off = new TaughtPoseFinder(null, null);
            var on = new TaughtPoseFinder(new FakeSource(), null);

            Assert.That(off.WhyNot("A-1"), Does.Contain("꺼져"));
            Assert.That(on.WhyNot(string.Empty), Does.Contain("칸 ID"));
            Assert.That(on.WhyNot("A-1"), Does.Contain("A-1"));
        }
    }
}
