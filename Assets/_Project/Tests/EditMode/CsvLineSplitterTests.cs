using NUnit.Framework;

namespace MediKitTwin.Orders.Tests
{
    public sealed class CsvLineSplitterTests
    {
        [Test]
        public void Split_PlainFields_TrimsEachField()
        {
            var fields = OrderTestFactory.Splitter().Split("1, A병원 ,EM, 2");

            Assert.That(fields, Is.EqualTo(new[] { "1", "A병원", "EM", "2" }));
        }

        [Test]
        public void Split_QuotedFieldWithDelimiter_KeepsFieldTogether()
        {
            var fields = OrderTestFactory.Splitter().Split("1,\"서울, 중앙병원\",EM,1");

            Assert.That(fields, Is.EqualTo(new[] { "1", "서울, 중앙병원", "EM", "1" }));
        }

        [Test]
        public void Split_EscapedQuote_ProducesSingleQuote()
        {
            var fields = OrderTestFactory.Splitter().Split("1,\"A \"\"큰\"\" 병원\",EM,1");

            Assert.That(fields[1], Is.EqualTo("A \"큰\" 병원"));
        }

        [Test]
        public void Split_TrailingDelimiter_KeepsEmptyLastField()
        {
            var fields = OrderTestFactory.Splitter().Split("1,A,EM,");

            Assert.That(fields.Count, Is.EqualTo(4));
            Assert.That(fields[3], Is.EqualTo(string.Empty));
        }

        [Test]
        public void Split_CustomDelimiter_UsesIt()
        {
            var fields = new CsvLineSplitter(';', '"').Split("1;A,B;EM;1");

            Assert.That(fields, Is.EqualTo(new[] { "1", "A,B", "EM", "1" }));
        }
    }
}
