using System.Collections.Generic;
using System.Linq;
using MediKitTwin.Common;
using MediKitTwin.Recipes;
using NUnit.Framework;

namespace MediKitTwin.Inspection.Tests
{
    public sealed class KitInspectionTests
    {
        private sealed class FakeReader : ISlotReader
        {
            private readonly string[] _ids;

            public FakeReader(params string[] ids)
            {
                _ids = ids;
            }

            public int ReadCount { get; private set; }

            public bool TryRead(int slotCount, out string[] componentIds)
            {
                ReadCount++;
                componentIds = _ids;
                return _ids != null;
            }
        }

        private sealed class FakePrimary : IInspector
        {
            private readonly bool _canJudge;

            public FakePrimary(bool canJudge)
            {
                _canJudge = canJudge;
            }

            public JudgeMethod Method => JudgeMethod.Cnn;

            public bool TryJudge(KitData kit, out InspectionVerdict verdict)
            {
                verdict = _canJudge ? new InspectionVerdict(true, new List<int>(), 0.97f, JudgeMethod.Cnn) : default;
                return _canJudge;
            }
        }

        private RecipeSO _recipe;
        private List<string> _warnings;

        [SetUp]
        public void SetUp()
        {
            _recipe = InspectionTestFactory.Recipe("EM", "gauze", "bandage", "antiseptic", "gloves");
            _warnings = new List<string>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_recipe);
        }

        private KitInspection Create(IInspector primary = null)
        {
            return new KitInspection(code => code == "EM" ? _recipe : null, primary, (code, message) => _warnings.Add(code));
        }

        private static KitData NewKit(string kitCode = "EM") => new KitData(1, 1, "L-1", "병원", kitCode);

        [Test]
        public void SensorsRead_FillsDetectedIds_AndJudgesByRule()
        {
            KitData kit = NewKit();
            var reader = new FakeReader("gauze", "bandage", null, "gloves");

            InspectionVerdict verdict = Create().Run(kit, reader);

            CollectionAssert.AreEqual(new[] { "gauze", "bandage", null, "gloves" }, kit.DetectedComponentIds);
            Assert.IsFalse(verdict.IsPass);
            CollectionAssert.AreEqual(new[] { 3 }, verdict.MissingSlots.ToArray());
            Assert.IsEmpty(_warnings);
        }

        [Test]
        public void SensorNoResponse_IsDefect_WithWarning_AndUnknownSlots()
        {
            KitData kit = NewKit();

            InspectionVerdict verdict = Create().Run(kit, new FakeReader((string[])null));

            Assert.IsFalse(verdict.IsPass);
            Assert.IsNull(verdict.MissingSlots);
            Assert.IsNull(kit.DetectedComponentIds);
            CollectionAssert.AreEqual(new[] { KitInspection.SensorWarningCode }, _warnings);
        }

        [Test]
        public void NoBoxInZone_IsTreatedAsSensorNoResponse()
        {
            InspectionVerdict verdict = Create().Run(NewKit(), null);

            Assert.IsFalse(verdict.IsPass);
            Assert.IsNull(verdict.MissingSlots);
            CollectionAssert.AreEqual(new[] { KitInspection.SensorWarningCode }, _warnings);
        }

        [Test]
        public void UnknownRecipe_IsDefect_WithWarning()
        {
            InspectionVerdict verdict = Create().Run(NewKit("ZZ"), new FakeReader("gauze"));

            Assert.IsFalse(verdict.IsPass);
            CollectionAssert.AreEqual(new[] { KitInspection.RecipeWarningCode }, _warnings);
        }

        [Test]
        public void PrimaryCanJudge_UsesItsVerdict_WithoutReadingSensors()
        {
            var reader = new FakeReader("gauze", "bandage", "antiseptic", "gloves");

            InspectionVerdict verdict = Create(new FakePrimary(true)).Run(NewKit(), reader);

            Assert.AreEqual(JudgeMethod.Cnn, verdict.Method);
            Assert.AreEqual(0, reader.ReadCount);
            Assert.IsEmpty(_warnings);
        }

        [Test]
        public void PrimaryCannotJudge_FallsBackToRule_WithWarning()
        {
            var reader = new FakeReader("gauze", "bandage", "antiseptic", "gloves");

            InspectionVerdict verdict = Create(new FakePrimary(false)).Run(NewKit(), reader);

            Assert.AreEqual(JudgeMethod.Rule, verdict.Method);
            Assert.IsTrue(verdict.IsPass);
            CollectionAssert.AreEqual(new[] { KitInspection.FallbackWarningCode }, _warnings);
        }
    }
}
