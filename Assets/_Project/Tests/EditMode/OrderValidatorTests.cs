using System;
using NUnit.Framework;

namespace MediKitTwin.Orders.Tests
{
    public sealed class OrderValidatorTests
    {
        private static OrderRecord Order(int sequence, string kitCode, int quantity)
        {
            return new OrderRecord(sequence, "병원", kitCode, quantity);
        }

        [Test]
        public void KnownKitCode_RegisteredCode_Passes()
        {
            var validator = new KnownKitCodeValidator(new[] { "em", " DX " });

            Assert.That(validator.TryValidate(Order(1, "EM", 1), 1, new OrderValidationContext(), out _), Is.True);
            Assert.That(validator.TryValidate(Order(2, "DX", 1), 2, new OrderValidationContext(), out _), Is.True);
        }

        [Test]
        public void KnownKitCode_UnregisteredCode_FailsWithCodeAndLineNumber()
        {
            var validator = new KnownKitCodeValidator(new[] { "EM" });

            bool isValid = validator.TryValidate(Order(1, "XX", 1), 7, new OrderValidationContext(), out OrderIssue issue);

            Assert.That(isValid, Is.False);
            Assert.That(issue.Code, Is.EqualTo(OrderIssueCodes.UnknownKitCode));
            StringAssert.Contains("7행", issue.Message);
        }

        [Test]
        public void KnownKitCode_NewRecipeCode_PassesWithoutCodeChange()
        {
            // NFR-07: 키트 종류는 코드 목록만 늘리면 되고 클래스는 고치지 않는다.
            var validator = new KnownKitCodeValidator(new[] { "EM", "BL", "DX", "NEW" });

            Assert.That(validator.TryValidate(Order(1, "NEW", 1), 1, new OrderValidationContext(), out _), Is.True);
        }

        [TestCase(1, true)]
        [TestCase(10, true)]
        [TestCase(0, false)]
        [TestCase(11, false)]
        public void QuantityRange_Boundaries(int quantity, bool expected)
        {
            var validator = new QuantityRangeValidator(1, 10);

            bool isValid = validator.TryValidate(Order(1, "EM", quantity), 1, new OrderValidationContext(), out OrderIssue issue);

            Assert.That(isValid, Is.EqualTo(expected));
            if (!expected)
            {
                Assert.That(issue.Code, Is.EqualTo(OrderIssueCodes.QuantityOutOfRange));
            }
        }

        [Test]
        public void QuantityRange_InvalidRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new QuantityRangeValidator(0, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new QuantityRangeValidator(5, 4));
        }

        [Test]
        public void UniqueSequence_SecondUseOfSameSequence_Fails()
        {
            var validator = new UniqueSequenceValidator();
            var context = new OrderValidationContext();

            Assert.That(validator.TryValidate(Order(1, "EM", 1), 1, context, out _), Is.True);
            bool isValid = validator.TryValidate(Order(1, "BL", 1), 2, context, out OrderIssue issue);

            Assert.That(isValid, Is.False);
            Assert.That(issue.Code, Is.EqualTo(OrderIssueCodes.DuplicateSequence));
        }

        [Test]
        public void UniqueSequence_NewContext_DoesNotRememberPreviousParse()
        {
            var validator = new UniqueSequenceValidator();

            validator.TryValidate(Order(1, "EM", 1), 1, new OrderValidationContext(), out _);

            Assert.That(validator.TryValidate(Order(1, "EM", 1), 1, new OrderValidationContext(), out _), Is.True);
        }
    }
}
