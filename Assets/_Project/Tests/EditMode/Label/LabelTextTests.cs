using MediKitTwin.Common;
using NUnit.Framework;

namespace MediKitTwin.Label.Tests
{
    public sealed class LabelTextTests
    {
        [Test]
        public void Compose_PutsHospitalLotAndKitCode_OnePerLine()
        {
            var kit = new KitData(1, 1, "LOT-0001", "시험병원", "EM");

            string[] lines = LabelText.Compose(kit);

            Assert.That(lines, Is.EqualTo(new[] { "시험병원", "LOT-0001", "EM" }));
        }

        [Test]
        public void Compose_AlwaysGivesTheSameNumberOfLines()
        {
            Assert.That(LabelText.Compose(new KitData(1, 1, "LOT-1", "병원", "DX")), Has.Length.EqualTo(LabelText.LineCount));
            Assert.That(LabelText.Compose(null), Has.Length.EqualTo(LabelText.LineCount));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Compose_ShowsADashForAnEmptyHospitalName_SoNoLineIsBlank(string hospital)
        {
            var kit = new KitData(1, 1, "LOT-1", hospital, "EM");

            string[] lines = LabelText.Compose(kit);

            Assert.That(lines[0], Is.EqualTo("-"));
        }

        [Test]
        public void Join_PutsLinesOnSeparateRows()
        {
            Assert.That(LabelText.Join(new[] { "a", "b", "c" }), Is.EqualTo("a\nb\nc"));
        }
    }
}
