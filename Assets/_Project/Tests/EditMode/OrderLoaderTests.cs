using NUnit.Framework;

namespace MediKitTwin.Orders.Tests
{
    public sealed class OrderLoaderTests
    {
        private static OrderLoader Loader(IOrderTextSource source)
        {
            return new OrderLoader(source, OrderTestFactory.ParserWithDefaultRules(), OrderTestFactory.DefaultOrders());
        }

        [Test]
        public void Load_ValidFile_ReturnsParsedOrders()
        {
            var result = Loader(new StringOrderTextSource("1,A,EM,2")).Load();

            Assert.That(result.Orders.Count, Is.EqualTo(1));
            Assert.That(result.Issues, Is.Empty);
        }

        [Test]
        public void Load_MissingFile_FallsBackToOneOrderPerKitCodeWithWarning()
        {
            var result = Loader(new FileOrderTextSource("no/such/orders.csv")).Load();

            Assert.That(result.Orders.Count, Is.EqualTo(OrderTestFactory.KitCodes.Length));
            Assert.That(OrderTestFactory.Codes(result.Issues), Is.EqualTo(new[] { OrderIssueCodes.FileMissing }));
        }

        [Test]
        public void Load_NoValidRow_FallsBackToDefaultsKeepingRowWarnings()
        {
            var result = Loader(new StringOrderTextSource("1,A,ZZ,1\n2,B,EM,99")).Load();

            Assert.That(result.Orders.Count, Is.EqualTo(OrderTestFactory.KitCodes.Length));
            Assert.That(OrderTestFactory.Codes(result.Issues), Is.EqualTo(new[]
            {
                OrderIssueCodes.UnknownKitCode,
                OrderIssueCodes.QuantityOutOfRange,
                OrderIssueCodes.NoValidOrder,
            }));
        }

        [Test]
        public void Load_SomeValidRows_DoesNotUseDefaults()
        {
            var result = Loader(new StringOrderTextSource("1,A,ZZ,1\n2,B,EM,1")).Load();

            Assert.That(result.Orders.Count, Is.EqualTo(1));
            Assert.That(result.Orders[0].Sequence, Is.EqualTo(2));
        }

        [Test]
        public void DefaultOrders_AreSortedByKitCodeWithSequentialNumbers()
        {
            var orders = OrderTestFactory.DefaultOrders().CreateDefaultOrders();

            Assert.That(orders[0].KitCode, Is.EqualTo("BL"));
            Assert.That(orders[1].KitCode, Is.EqualTo("DX"));
            Assert.That(orders[2].KitCode, Is.EqualTo("EM"));
            Assert.That(orders[2].Sequence, Is.EqualTo(3));
        }

        [Test]
        public void Constructor_NullDependency_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new OrderLoader(null, OrderTestFactory.ParserWithDefaultRules(), OrderTestFactory.DefaultOrders()));
        }
    }
}
