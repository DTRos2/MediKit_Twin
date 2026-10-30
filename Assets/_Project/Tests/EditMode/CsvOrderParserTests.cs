using System.Collections.Generic;
using NUnit.Framework;

namespace MediKitTwin.Orders.Tests
{
    public sealed class CsvOrderParserTests
    {
        [Test]
        public void Parse_ValidRows_KeepsFileOrderAndNormalizesKitCode()
        {
            var result = OrderTestFactory.ParserWithDefaultRules().Parse("1,A병원,em,2\n2,B병원,DX,3\n");

            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Orders.Count, Is.EqualTo(2));
            Assert.That(result.Orders[0].KitCode, Is.EqualTo("EM"));
            Assert.That(result.Orders[1].Sequence, Is.EqualTo(2));
            Assert.That(result.Orders[1].Quantity, Is.EqualTo(3));
        }

        [Test]
        public void Parse_HeaderLine_IsSkippedWithoutIssue()
        {
            var result = OrderTestFactory.ParserWithDefaultRules().Parse("순번,병원명,키트 코드,수량\n1,A,EM,1");

            Assert.That(result.Orders.Count, Is.EqualTo(1));
            Assert.That(result.Issues, Is.Empty);
        }

        [Test]
        public void Parse_BomBlankAndCommentLines_AreIgnored()
        {
            var result = OrderTestFactory.ParserWithDefaultRules().Parse("﻿# 주문\n\n1,A,EM,1\r\n\r\n");

            Assert.That(result.Orders.Count, Is.EqualTo(1));
            Assert.That(result.Issues, Is.Empty);
        }

        [Test]
        public void Parse_NonNumericSequenceAfterFirstLine_ReportsInvalidRow()
        {
            var result = OrderTestFactory.ParserWithDefaultRules().Parse("1,A,EM,1\nabc,B,EM,1");

            Assert.That(result.Orders.Count, Is.EqualTo(1));
            Assert.That(OrderTestFactory.Codes(result.Issues), Is.EqualTo(new[] { OrderIssueCodes.InvalidRow }));
            StringAssert.Contains("2행", result.Issues[0].Message);
        }

        [Test]
        public void Parse_WrongColumnCount_ReportsInvalidRow()
        {
            var result = OrderTestFactory.ParserWithDefaultRules().Parse("1,A,EM\n2,B,EM,1,extra\n3,C,EM,1");

            Assert.That(result.Orders.Count, Is.EqualTo(1));
            Assert.That(OrderTestFactory.Codes(result.Issues),
                Is.EqualTo(new[] { OrderIssueCodes.InvalidRow, OrderIssueCodes.InvalidRow }));
        }

        [Test]
        public void Parse_NonNumericQuantity_ReportsInvalidRow()
        {
            var result = OrderTestFactory.ParserWithDefaultRules().Parse("1,A,EM,many");

            Assert.That(result.Orders, Is.Empty);
            Assert.That(OrderTestFactory.Codes(result.Issues), Is.EqualTo(new[] { OrderIssueCodes.InvalidRow }));
        }

        [Test]
        public void Parse_RejectedRows_ReportEachValidatorCode()
        {
            var result = OrderTestFactory.ParserWithDefaultRules().Parse("1,A,EM,2\n2,B,XX,1\n3,C,DX,11\n1,D,BL,1\n");

            Assert.That(result.Orders.Count, Is.EqualTo(1));
            Assert.That(OrderTestFactory.Codes(result.Issues), Is.EqualTo(new List<string>
            {
                OrderIssueCodes.UnknownKitCode,
                OrderIssueCodes.QuantityOutOfRange,
                OrderIssueCodes.DuplicateSequence,
            }));
        }

        [Test]
        public void Parse_RowRejectedByEarlierValidator_DoesNotReserveItsSequence()
        {
            // 알 수 없는 코드로 거부된 2번이 순번을 차지하면 뒤의 유효한 2번까지 중복으로 잘못 거부된다.
            var result = OrderTestFactory.ParserWithDefaultRules().Parse("2,A,XX,1\n2,B,EM,1");

            Assert.That(result.Orders.Count, Is.EqualTo(1));
            Assert.That(result.Orders[0].HospitalName, Is.EqualTo("B"));
        }

        [Test]
        public void Parse_EmptyHospitalName_IsAccepted()
        {
            var result = OrderTestFactory.ParserWithDefaultRules().Parse("1,,EM,1");

            Assert.That(result.Orders.Count, Is.EqualTo(1));
            Assert.That(result.Orders[0].HospitalName, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Parse_NoValidators_AcceptsEveryWellFormedRow()
        {
            var result = OrderTestFactory.Parser().Parse("1,A,ZZ,999");

            Assert.That(result.Orders.Count, Is.EqualTo(1));
        }

        [Test]
        public void Parse_CustomColumnLayout_ReadsByConfiguredIndexes()
        {
            var parser = new CsvOrderParser(
                OrderTestFactory.Splitter(),
                new CsvOrderColumns(sequenceIndex: 3, hospitalNameIndex: 0, kitCodeIndex: 1, quantityIndex: 2),
                OrderTestFactory.CommentMarker,
                new IOrderRowValidator[0]);

            var result = parser.Parse("A병원,EM,4,1");

            Assert.That(result.Orders.Count, Is.EqualTo(1));
            Assert.That(result.Orders[0].Sequence, Is.EqualTo(1));
            Assert.That(result.Orders[0].HospitalName, Is.EqualTo("A병원"));
            Assert.That(result.Orders[0].Quantity, Is.EqualTo(4));
        }

        [Test]
        public void Parse_CustomValidator_IsAppliedWithoutChangingParser()
        {
            var parser = OrderTestFactory.Parser(new RejectHospitalValidator("금지병원"));

            var result = parser.Parse("1,금지병원,EM,1\n2,A,EM,1");

            Assert.That(result.Orders.Count, Is.EqualTo(1));
            Assert.That(result.Issues.Count, Is.EqualTo(1));
        }

        [Test]
        public void Parse_NullText_ReturnsEmptyResult()
        {
            var result = OrderTestFactory.ParserWithDefaultRules().Parse(null);

            Assert.That(result.Orders, Is.Empty);
            Assert.That(result.Issues, Is.Empty);
        }

        private sealed class RejectHospitalValidator : IOrderRowValidator
        {
            private readonly string _blocked;

            public RejectHospitalValidator(string blocked)
            {
                _blocked = blocked;
            }

            public bool TryValidate(OrderRecord order, int lineNumber, OrderValidationContext context, out OrderIssue issue)
            {
                bool isBlocked = order.HospitalName == _blocked;
                issue = isBlocked ? new OrderIssue("TEST_BLOCKED", "blocked") : default;
                return !isBlocked;
            }
        }
    }
}
