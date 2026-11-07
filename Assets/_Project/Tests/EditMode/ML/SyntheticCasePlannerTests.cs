using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MediKitTwin.ML.Tests
{
    public sealed class SyntheticCasePlannerTests
    {
        private static List<SyntheticKitSpec> Kits()
        {
            return new List<SyntheticKitSpec>
            {
                new SyntheticKitSpec("EM", new[] { "gauze", "bandage", "antiseptic", "gloves" }),
                new SyntheticKitSpec("BL", new[] { "blood_tube", "needle", "alcohol_pad", "tourniquet" }),
                new SyntheticKitSpec("DX", new[] { "swab", "buffer_vial", "test_cassette", "sample_bag" }),
            };
        }

        [Test]
        public void TestSet_HasSameNumberOfNormalAndDefect()
        {
            var cases = SyntheticCasePlanner.Plan(Kits(), DataSplit.Test, 120, 1);

            Assert.AreEqual(120, cases.Count);
            Assert.AreEqual(60, cases.Count(c => !c.IsDefect));
            Assert.AreEqual(60, cases.Count(c => c.IsDefect));
        }

        [Test]
        public void TestSet_OddCount_Throws()
        {
            Assert.Throws<ArgumentException>(() => SyntheticCasePlanner.Plan(Kits(), DataSplit.Test, 121, 1));
        }

        [Test]
        public void Defects_SplitIntoMissingAndWrongItemEvenly()
        {
            var cases = SyntheticCasePlanner.Plan(Kits(), DataSplit.Train, 100, 2);

            Assert.AreEqual(50, cases.Count(c => c.Variant == BoxVariant.Normal));
            Assert.AreEqual(25, cases.Count(c => c.Variant == BoxVariant.Missing));
            Assert.AreEqual(25, cases.Count(c => c.Variant == BoxVariant.WrongItem));
        }

        [Test]
        public void EachVariant_UsesAllKitsEvenly()
        {
            var cases = SyntheticCasePlanner.Plan(Kits(), DataSplit.Test, 180, 3);

            foreach (BoxVariant variant in Enum.GetValues(typeof(BoxVariant)))
            {
                var perKit = cases.Where(c => c.Variant == variant).GroupBy(c => c.KitCode).Select(g => g.Count()).ToList();
                Assert.AreEqual(3, perKit.Count, variant.ToString());
                Assert.LessOrEqual(perKit.Max() - perKit.Min(), 1, variant.ToString());
            }
        }

        [Test]
        public void SameSeed_SamePlan_DifferentSeed_DifferentPlan()
        {
            var a = SyntheticCasePlanner.Plan(Kits(), DataSplit.Train, 50, 7);
            var b = SyntheticCasePlanner.Plan(Kits(), DataSplit.Train, 50, 7);
            var c = SyntheticCasePlanner.Plan(Kits(), DataSplit.Train, 50, 8);

            Assert.AreEqual(Describe(a), Describe(b));
            Assert.AreNotEqual(Describe(a), Describe(c));
        }

        [Test]
        public void NormalCase_HasNoAffectedSlot_AndMatchesRecipe()
        {
            var cases = SyntheticCasePlanner.Plan(Kits(), DataSplit.Train, 30, 4);

            foreach (SyntheticBoxCase boxCase in cases.Where(c => c.Variant == BoxVariant.Normal))
            {
                Assert.AreEqual(0, boxCase.AffectedSlotNumber);
                for (int slot = 1; slot <= boxCase.SlotCount; slot++)
                {
                    Assert.AreEqual(boxCase.GetExpectedComponentId(slot), boxCase.GetActualComponentId(slot));
                }
            }
        }

        [Test]
        public void MissingCase_EmptiesExactlyOneSlot()
        {
            var cases = SyntheticCasePlanner.Plan(Kits(), DataSplit.Train, 60, 5);

            foreach (SyntheticBoxCase boxCase in cases.Where(c => c.Variant == BoxVariant.Missing))
            {
                Assert.AreEqual(SlotCropRecord.EmptyLabel, boxCase.GetActualComponentId(boxCase.AffectedSlotNumber));
                Assert.AreEqual(1, Differences(boxCase).Count);
                Assert.AreEqual(boxCase.AffectedSlotNumber, Differences(boxCase)[0]);
            }
        }

        [Test]
        public void WrongItemCase_ReplacesOneSlotWithComponentNotInTheBox()
        {
            var cases = SyntheticCasePlanner.Plan(Kits(), DataSplit.Train, 200, 6);

            foreach (SyntheticBoxCase boxCase in cases.Where(c => c.Variant == BoxVariant.WrongItem))
            {
                Assert.AreEqual(1, Differences(boxCase).Count);
                int slot = boxCase.AffectedSlotNumber;
                string wrong = boxCase.GetActualComponentId(slot);

                Assert.AreNotEqual(SlotCropRecord.EmptyLabel, wrong);
                Assert.AreNotEqual(boxCase.GetExpectedComponentId(slot), wrong);
                for (int other = 1; other <= boxCase.SlotCount; other++)
                {
                    Assert.AreNotEqual(boxCase.GetExpectedComponentId(other), wrong, "같은 박스의 다른 슬롯 구성품과 겹친다.");
                }
            }
        }

        [Test]
        public void WrongItem_WithSingleKit_Throws()
        {
            var single = new List<SyntheticKitSpec> { Kits()[0] };

            Assert.Throws<InvalidOperationException>(() => SyntheticCasePlanner.Plan(single, DataSplit.Train, 10, 1));
        }

        [Test]
        public void SpecWithoutComponents_Throws()
        {
            Assert.Throws<ArgumentException>(() => new SyntheticKitSpec("EM", new string[0]));
            Assert.Throws<ArgumentException>(() => new SyntheticKitSpec("", new[] { "gauze" }));
        }

        private static List<int> Differences(SyntheticBoxCase boxCase)
        {
            var slots = new List<int>();
            for (int slot = 1; slot <= boxCase.SlotCount; slot++)
            {
                if (boxCase.GetExpectedComponentId(slot) != boxCase.GetActualComponentId(slot))
                {
                    slots.Add(slot);
                }
            }

            return slots;
        }

        private static string Describe(List<SyntheticBoxCase> cases)
        {
            return string.Join(";", cases.Select(c => $"{c.Index}:{c.KitCode}:{c.Variant}:{c.AffectedSlotNumber}:{c.Seed}:{string.Join("|", Enumerable.Range(1, c.SlotCount).Select(c.GetActualComponentId))}"));
        }
    }
}
