using System;
using NUnit.Framework;

namespace MediKitTwin.Pusher.Tests
{
    public sealed class RejectBinTallyTests
    {
        [Test]
        public void DefaultCapacity_IsFive()
        {
            Assert.AreEqual(5, new RejectBinTally().Capacity);
        }

        [Test]
        public void AddLoaded_BelowCapacity_DoesNotClear()
        {
            var tally = new RejectBinTally();

            for (int i = 1; i <= 4; i++)
            {
                Assert.IsFalse(tally.AddLoaded());
                Assert.AreEqual(i, tally.Loaded);
            }

            Assert.AreEqual(0, tally.ClearCount);
        }

        [Test]
        public void AddLoaded_ReachingCapacity_ClearsAndResetsLoaded()
        {
            var tally = new RejectBinTally();
            for (int i = 0; i < 4; i++)
            {
                tally.AddLoaded();
            }

            Assert.IsTrue(tally.AddLoaded());
            Assert.AreEqual(0, tally.Loaded);
            Assert.AreEqual(5, tally.Total);
            Assert.AreEqual(1, tally.ClearCount);
        }

        [Test]
        public void SixDefects_ClearsAtFifth_AndTotalIsSix()
        {
            // SRS FR-09 시험: 불량 6개가 발생하면 5개째에 불량 박스가 비워지고 누적 불량 수는 6이다.
            var tally = new RejectBinTally();
            int clearedAt = 0;

            for (int i = 1; i <= 6; i++)
            {
                if (tally.AddLoaded())
                {
                    clearedAt = i;
                }
            }

            Assert.AreEqual(5, clearedAt);
            Assert.AreEqual(6, tally.Total);
            Assert.AreEqual(1, tally.Loaded);
            Assert.AreEqual(1, tally.ClearCount);
        }

        [Test]
        public void TenDefects_ClearsTwice()
        {
            var tally = new RejectBinTally();

            for (int i = 0; i < 10; i++)
            {
                tally.AddLoaded();
            }

            Assert.AreEqual(2, tally.ClearCount);
            Assert.AreEqual(0, tally.Loaded);
            Assert.AreEqual(10, tally.Total);
        }

        [Test]
        public void AddOutside_CountsInTotalOnly()
        {
            var tally = new RejectBinTally();
            tally.AddLoaded();

            tally.AddOutside();

            Assert.AreEqual(1, tally.Loaded);
            Assert.AreEqual(2, tally.Total);
            Assert.AreEqual(0, tally.ClearCount);
        }

        [Test]
        public void CapacityOne_ClearsEveryTime()
        {
            var tally = new RejectBinTally(1);

            Assert.IsTrue(tally.AddLoaded());
            Assert.IsTrue(tally.AddLoaded());
            Assert.AreEqual(2, tally.ClearCount);
        }

        [Test]
        public void Capacity_BelowOne_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RejectBinTally(0));
        }
    }
}
