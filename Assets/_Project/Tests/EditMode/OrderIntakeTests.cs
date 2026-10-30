using System.Linq;
using NUnit.Framework;

namespace MediKitTwin.Orders.Tests
{
    /// <summary>TC-01: 수량 합이 9인 3행 주문 파일로 주문 3건, 키트 9개가 파일 순서대로 등록되는지 확인한다.</summary>
    public sealed class OrderIntakeTests
    {
        private const string MixedOrders = "순번,병원명,키트 코드,수량\n1,A병원,EM,2\n2,B병원,DX,3\n3,C병원,BL,4\n";

        [Test]
        public void Run_MixedOrderFile_CreatesThreeOrdersAndNineKitsInFileOrder()
        {
            var result = OrderTestFactory.Intake(new StringOrderTextSource(MixedOrders)).Run();

            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Orders.Count, Is.EqualTo(3));
            Assert.That(result.KitRequests.Count, Is.EqualTo(9));
            Assert.That(result.KitRequests.Select(k => k.KitCode),
                Is.EqualTo(new[] { "EM", "EM", "DX", "DX", "DX", "BL", "BL", "BL", "BL" }));
        }

        [Test]
        public void Run_MixedOrderFile_EveryKitHasUniqueLotAndItsOrderHospital()
        {
            var result = OrderTestFactory.Intake(new StringOrderTextSource(MixedOrders)).Run();

            Assert.That(result.KitRequests.Select(k => k.LotNumber).Distinct().Count(), Is.EqualTo(9));
            Assert.That(result.KitRequests[2].HospitalName, Is.EqualTo("B병원"));
            Assert.That(result.KitRequests[8].HospitalName, Is.EqualTo("C병원"));
        }

        [Test]
        public void Run_SameFileTwice_GivesSameResult()
        {
            // NFR-06: 같은 주문 파일이면 키트 순서와 로트번호가 같아야 같은 시드에서 같은 키트가 누락된다.
            var first = OrderTestFactory.Intake(new StringOrderTextSource(MixedOrders)).Run();
            var second = OrderTestFactory.Intake(new StringOrderTextSource(MixedOrders)).Run();

            Assert.That(second.KitRequests.Select(k => k.LotNumber), Is.EqualTo(first.KitRequests.Select(k => k.LotNumber)));
            Assert.That(second.KitRequests.Select(k => k.KitCode), Is.EqualTo(first.KitRequests.Select(k => k.KitCode)));
        }

        [Test]
        public void Run_MissingFile_UsesDefaultSetAndReportsIt()
        {
            var result = OrderTestFactory.Intake(new FileOrderTextSource("no/such/orders.csv")).Run();

            Assert.That(result.KitRequests.Count, Is.EqualTo(3));
            Assert.That(result.Issues.Single().Code, Is.EqualTo(OrderIssueCodes.FileMissing));
        }
    }
}
