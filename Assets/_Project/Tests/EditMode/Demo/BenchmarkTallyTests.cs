using NUnit.Framework;

namespace MediKitTwin.Demo.Tests
{
    public sealed class BenchmarkTallyTests
    {
        [Test]
        public void Empty_HasZeroRatesAndDoesNotPass()
        {
            var tally = new BenchmarkTally();

            Assert.AreEqual(0, tally.Boxes);
            Assert.AreEqual(0f, tally.BoxAccuracy);
            Assert.IsFalse(tally.Passed);
        }

        [Test]
        public void Counts_AllFourOutcomes()
        {
            var tally = new BenchmarkTally();
            tally.Add(true, new[] { 2 }, false, new[] { 2 });  // 불량을 잡음
            tally.Add(true, new[] { 3 }, true, new int[0]);    // 불량을 놓침
            tally.Add(false, new int[0], true, new int[0]);    // 정상을 통과
            tally.Add(false, new int[0], false, new[] { 1 });  // 정상을 불량으로 오경보

            Assert.AreEqual(4, tally.Boxes);
            Assert.AreEqual(0.5f, tally.BoxAccuracy, 1e-6f);
            Assert.AreEqual(0.5f, tally.DefectRecall, 1e-6f);
            Assert.AreEqual(0.5f, tally.FalseAlarmRate, 1e-6f);
            Assert.AreEqual(1, tally.MissedDefects);
            Assert.AreEqual(1, tally.FalseAlarms);
        }

        [Test]
        public void Add_ReturnsWhetherBoxVerdictWasRight()
        {
            var tally = new BenchmarkTally();

            Assert.IsTrue(tally.Add(true, new[] { 1 }, false, new[] { 4 }), "슬롯이 달라도 불량은 맞혔다");
            Assert.IsFalse(tally.Add(false, new int[0], false, new[] { 1 }));
        }

        [Test]
        public void SlotLocalization_RequiresExactSlotSet()
        {
            var tally = new BenchmarkTally();
            tally.Add(true, new[] { 2 }, false, new[] { 2 });
            tally.Add(true, new[] { 2 }, false, new[] { 1, 2 });
            tally.Add(true, new[] { 2 }, false, null);

            Assert.AreEqual(1f / 3f, tally.SlotLocalization, 1e-6f);
        }

        [Test]
        public void Passed_NeedsBothRatesAtLeastNinetyPercent()
        {
            var tally = new BenchmarkTally();
            for (int i = 0; i < 9; i++)
            {
                tally.Add(true, new[] { 1 }, false, new[] { 1 });
                tally.Add(false, new int[0], true, new int[0]);
            }

            tally.Add(true, new[] { 1 }, true, new int[0]); // 재현율 9/10

            Assert.AreEqual(0.9f, tally.DefectRecall, 1e-6f);
            Assert.IsTrue(tally.Passed);

            tally.Add(true, new[] { 1 }, true, new int[0]); // 9/11 < 0.9
            Assert.IsFalse(tally.Passed);
        }
    }
}
