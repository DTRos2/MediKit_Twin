using System.Collections.Generic;
using MediKitTwin.Common;
using MediKitTwin.Recipes;
using NUnit.Framework;
using UnityEngine;

namespace MediKitTwin.Inspection.Tests
{
    public sealed class CnnSlotJudgeTests
    {
        private RecipeSO _recipe;
        private FakeCropSource _crops;
        private FakeClassifier _classifier;
        private CnnSlotJudge _judge;

        [SetUp]
        public void SetUp()
        {
            _recipe = InspectionTestFactory.Recipe("EM", "gauze", "bandage", "antiseptic");
            _crops = new FakeCropSource(3);
            _classifier = new FakeClassifier();
            _judge = new CnnSlotJudge(code => code == "EM" ? _recipe : null, _crops, _classifier);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_recipe);
        }

        [Test]
        public void AllSlotsMatch_IsPass_UsesLowestConfidence()
        {
            _classifier.Result = new[] { P("gauze", 0.99f), P("bandage", 0.80f), P("antiseptic", 0.95f) };

            Assert.IsTrue(_judge.TryJudge(InspectionTestFactory.Kit("EM"), out InspectionVerdict verdict));

            Assert.IsTrue(verdict.IsPass);
            Assert.AreEqual(0, verdict.MissingSlots.Count);
            Assert.AreEqual(0.80f, verdict.Confidence, 1e-6f);
            Assert.AreEqual(JudgeMethod.Cnn, verdict.Method);
        }

        [Test]
        public void EmptySlot_IsDefect_WithThatSlot()
        {
            _classifier.Result = new[] { P("gauze", 0.99f), P("empty", 0.9f), P("antiseptic", 0.95f) };

            _judge.TryJudge(InspectionTestFactory.Kit("EM"), out InspectionVerdict verdict);

            Assert.IsFalse(verdict.IsPass);
            CollectionAssert.AreEqual(new[] { 2 }, verdict.MissingSlots);
        }

        [Test]
        public void WrongComponent_IsDefect_WithThatSlot()
        {
            _classifier.Result = new[] { P("gauze", 0.99f), P("bandage", 0.9f), P("swab", 0.95f) };

            _judge.TryJudge(InspectionTestFactory.Kit("EM"), out InspectionVerdict verdict);

            Assert.IsFalse(verdict.IsPass);
            CollectionAssert.AreEqual(new[] { 3 }, verdict.MissingSlots);
        }

        [Test]
        public void UnknownRecipe_CannotJudge()
        {
            Assert.IsFalse(_judge.TryJudge(InspectionTestFactory.Kit("XX"), out _));
        }

        [Test]
        public void CropsUnavailable_CannotJudge()
        {
            _crops.Available = false;

            Assert.IsFalse(_judge.TryJudge(InspectionTestFactory.Kit("EM"), out _));
        }

        [Test]
        public void ClassifierFails_CannotJudge()
        {
            _classifier.Result = null;

            Assert.IsFalse(_judge.TryJudge(InspectionTestFactory.Kit("EM"), out _));
        }

        [Test]
        public void PredictionCountMismatch_CannotJudge()
        {
            _classifier.Result = new[] { P("gauze", 1f) };

            Assert.IsFalse(_judge.TryJudge(InspectionTestFactory.Kit("EM"), out _));
        }

        private static SlotPrediction P(string label, float confidence) => new SlotPrediction(label, confidence);

        private sealed class FakeCropSource : ISlotCropSource
        {
            private readonly int _count;

            public FakeCropSource(int count)
            {
                _count = count;
            }

            public bool Available = true;

            public bool TryGetCrops(KitData kit, RecipeSO recipe, out SlotCropSet crops)
            {
                crops = null;
                if (!Available)
                {
                    return false;
                }

                var list = new List<SlotCrop>();
                for (int i = 1; i <= _count; i++)
                {
                    list.Add(new SlotCrop(i, new RectInt(0, 0, 2, 2), new Texture2D(2, 2)));
                }

                crops = new SlotCropSet(list);
                return true;
            }
        }

        private sealed class FakeClassifier : ISlotClassifier
        {
            public SlotPrediction[] Result;

            public bool TryClassify(IReadOnlyList<Texture2D> crops, out SlotPrediction[] predictions)
            {
                predictions = Result;
                return Result != null;
            }
        }
    }
}
