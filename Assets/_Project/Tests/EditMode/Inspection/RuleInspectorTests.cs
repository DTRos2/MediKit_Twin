using System.Linq;
using MediKitTwin.Common;
using MediKitTwin.Recipes;
using NUnit.Framework;

namespace MediKitTwin.Inspection.Tests
{
    public sealed class RuleInspectorTests
    {
        private RecipeSO _recipe;
        private RuleInspector _inspector;

        [SetUp]
        public void SetUp()
        {
            _recipe = InspectionTestFactory.Recipe("EM", "gauze", "bandage", "antiseptic", "gloves");
            _inspector = new RuleInspector(code => code == "EM" ? _recipe : null);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_recipe);
        }

        [Test]
        public void AllSlotsMatch_IsPass_WithEmptyMissingList()
        {
            KitData kit = InspectionTestFactory.Kit("EM", "gauze", "bandage", "antiseptic", "gloves");

            Assert.IsTrue(_inspector.TryJudge(kit, out InspectionVerdict verdict));

            Assert.IsTrue(verdict.IsPass);
            Assert.AreEqual(0, verdict.MissingSlots.Count);
            Assert.AreEqual(1f, verdict.Confidence);
            Assert.AreEqual(JudgeMethod.Rule, verdict.Method);
        }

        [Test]
        public void EmptySlot_IsDefect_WithThatSlotNumber()
        {
            KitData kit = InspectionTestFactory.Kit("EM", "gauze", null, "antiseptic", "gloves");

            _inspector.TryJudge(kit, out InspectionVerdict verdict);

            Assert.IsFalse(verdict.IsPass);
            CollectionAssert.AreEqual(new[] { 2 }, verdict.MissingSlots.ToArray());
        }

        [Test]
        public void WrongComponent_IsDefect_WithThatSlotNumber()
        {
            KitData kit = InspectionTestFactory.Kit("EM", "gauze", "bandage", "swab", "gloves");

            _inspector.TryJudge(kit, out InspectionVerdict verdict);

            Assert.IsFalse(verdict.IsPass);
            CollectionAssert.AreEqual(new[] { 3 }, verdict.MissingSlots.ToArray());
        }

        [Test]
        public void MissingAndWrongTogether_ListsBothSlots()
        {
            KitData kit = InspectionTestFactory.Kit("EM", null, "bandage", "antiseptic", "swab");

            _inspector.TryJudge(kit, out InspectionVerdict verdict);

            CollectionAssert.AreEqual(new[] { 1, 4 }, verdict.MissingSlots.ToArray());
        }

        [Test]
        public void NoSensorResult_IsDefect_WithUnknownSlots()
        {
            KitData kit = InspectionTestFactory.Kit("EM", null);
            kit.DetectedComponentIds = null;

            Assert.IsTrue(_inspector.TryJudge(kit, out InspectionVerdict verdict));

            Assert.IsFalse(verdict.IsPass);
            Assert.IsNull(verdict.MissingSlots);
        }

        [Test]
        public void SlotCountMismatch_IsTreatedAsSensorFailure()
        {
            KitData kit = InspectionTestFactory.Kit("EM", "gauze", "bandage");

            _inspector.TryJudge(kit, out InspectionVerdict verdict);

            Assert.IsFalse(verdict.IsPass);
            Assert.IsNull(verdict.MissingSlots);
        }

        [Test]
        public void UnknownKitCode_CannotJudge()
        {
            KitData kit = InspectionTestFactory.Kit("ZZ", "gauze");

            Assert.IsFalse(_inspector.TryJudge(kit, out _));
        }

        [Test]
        public void Judging_DoesNotReadTheAnswerLabel()
        {
            // 정답은 슬롯 2가 누락이라고 기록해 두어도, 판정은 센서 결과(모두 정상)만 본다.
            KitData kit = InspectionTestFactory.Kit("EM", "gauze", "bandage", "antiseptic", "gloves");
            kit.SetMissingAnswer(2);

            _inspector.TryJudge(kit, out InspectionVerdict verdict);

            Assert.IsTrue(verdict.IsPass);
        }
    }
}
