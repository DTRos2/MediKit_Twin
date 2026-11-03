using NUnit.Framework;

namespace MediKitTwin.Inspection.Tests
{
    public sealed class SlotClassMapTests
    {
        private const string Valid =
            "{\"classes\":[\"alcohol_pad\",\"empty\",\"gauze\"],\"input\":{\"width\":128,\"height\":72,\"channels\":3,\"layout\":\"NCHW\",\"range\":\"0..1 RGB\"}}";

        [Test]
        public void Valid_KeepsClassOrder()
        {
            Assert.IsTrue(SlotClassMap.TryParse(Valid, out SlotClassMap map));

            Assert.AreEqual(3, map.Count);
            Assert.AreEqual("empty", map.LabelAt(1));
        }

        [Test]
        public void InputSizeMismatch_IsRejected()
        {
            string json = Valid.Replace("\"width\":128", "\"width\":64");

            Assert.IsFalse(SlotClassMap.TryParse(json, out _));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not json")]
        [TestCase("{\"classes\":[\"a\"]}")]
        public void Broken_IsRejected(string json)
        {
            Assert.IsFalse(SlotClassMap.TryParse(json, out _));
        }
    }
}
