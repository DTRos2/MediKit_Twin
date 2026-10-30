using System.Linq;
using NUnit.Framework;

namespace MediKitTwin.Orders.Tests
{
    public sealed class InFlightKitsTests
    {
        private static KitRequest Request(int order, string lot) => new KitRequest(order, "병원", "EM", lot);

        [Test]
        public void TakeByOrder_ReturnsOnlyThatOrdersKitsInKitSequenceOrderAndRemovesThem()
        {
            var inFlight = new InFlightKits();
            inFlight.Add(3, Request(2, "LOT-C"));
            inFlight.Add(1, Request(1, "LOT-A"));
            inFlight.Add(2, Request(2, "LOT-B"));

            var taken = inFlight.TakeByOrder(2);

            Assert.That(taken.Select(r => r.LotNumber), Is.EqualTo(new[] { "LOT-B", "LOT-C" }));
            Assert.That(inFlight.Count, Is.EqualTo(1));
        }

        [Test]
        public void TakeByOrder_NoKitsInFlight_ReturnsEmpty()
        {
            Assert.That(new InFlightKits().TakeByOrder(1), Is.Empty);
        }

        [Test]
        public void Complete_RemovesKit_AndUnknownSequenceIsIgnored()
        {
            var inFlight = new InFlightKits();
            inFlight.Add(1, Request(1, "LOT-A"));

            inFlight.Complete(1);
            inFlight.Complete(99);

            Assert.That(inFlight.Count, Is.EqualTo(0));
        }

        [Test]
        public void OrderSequences_ListsEachOrderOnceByKitSequence()
        {
            var inFlight = new InFlightKits();
            inFlight.Add(1, Request(5, "LOT-A"));
            inFlight.Add(2, Request(5, "LOT-B"));
            inFlight.Add(3, Request(7, "LOT-C"));

            Assert.That(inFlight.OrderSequences(), Is.EqualTo(new[] { 5, 7 }));
        }
    }
}
