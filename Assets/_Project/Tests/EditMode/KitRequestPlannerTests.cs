using System.Linq;
using NUnit.Framework;

namespace MediKitTwin.Orders.Tests
{
    public sealed class KitRequestPlannerTests
    {
        private static KitRequestPlanner Planner() => new KitRequestPlanner(new SequentialLotNumberGenerator("LOT-", 4));

        [Test]
        public void Plan_ExpandsEachOrderByQuantityInOrder()
        {
            var orders = new[]
            {
                new OrderRecord(1, "A", "EM", 2),
                new OrderRecord(2, "B", "DX", 1),
            };

            var requests = Planner().Plan(orders);

            Assert.That(requests.Select(r => r.KitCode), Is.EqualTo(new[] { "EM", "EM", "DX" }));
            Assert.That(requests.Select(r => r.OrderSequence), Is.EqualTo(new[] { 1, 1, 2 }));
        }

        [Test]
        public void Plan_AssignsUniqueLotNumbers()
        {
            var requests = Planner().Plan(new[] { new OrderRecord(1, "A", "EM", 3) });

            Assert.That(requests.Select(r => r.LotNumber), Is.EqualTo(new[] { "LOT-0001", "LOT-0002", "LOT-0003" }));
        }

        [Test]
        public void Plan_SameOrdersTwice_GivesSameLotsForSeparatePlanners()
        {
            var orders = new[] { new OrderRecord(1, "A", "EM", 2) };

            var first = Planner().Plan(orders).Select(r => r.LotNumber);
            var second = Planner().Plan(orders).Select(r => r.LotNumber);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void KitRequest_CreateKitData_CopiesOrderInfoAndTakesSequence()
        {
            var request = new KitRequest(2, "B병원", "DX", "LOT-0005");

            var kit = request.CreateKitData(7);

            Assert.That(kit.KitSequence, Is.EqualTo(7));
            Assert.That(kit.OrderSequence, Is.EqualTo(2));
            Assert.That(kit.HospitalName, Is.EqualTo("B병원"));
            Assert.That(kit.KitCode, Is.EqualTo("DX"));
            Assert.That(kit.LotNumber, Is.EqualTo("LOT-0005"));
        }

        [Test]
        public void KitRequest_CreateKitData_ReturnsFreshKitWithoutMissingAnswer()
        {
            var request = new KitRequest(1, "A", "EM", "LOT-0001");
            var first = request.CreateKitData(1);
            first.SetMissingAnswer(2);

            var resumed = request.CreateKitData(5);

            Assert.That(resumed, Is.Not.SameAs(first));
            Assert.That(resumed.GetMissingAnswerForEvaluation(), Is.Null);
        }

        [Test]
        public void LotNumber_PadsToConfiguredDigitsAndStartNumber()
        {
            var generator = new SequentialLotNumberGenerator("L", 3, 9);

            Assert.That(generator.Next(), Is.EqualTo("L009"));
            Assert.That(generator.Next(), Is.EqualTo("L010"));
        }
    }
}
