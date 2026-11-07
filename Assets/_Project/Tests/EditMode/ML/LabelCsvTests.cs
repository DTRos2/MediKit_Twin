using System.Collections.Generic;
using NUnit.Framework;

namespace MediKitTwin.ML.Tests
{
    public sealed class LabelCsvTests
    {
        private static SyntheticBoxCase MissingCase()
        {
            return new SyntheticBoxCase(
                12, DataSplit.Train, "EM", BoxVariant.Missing, 2,
                new[] { "gauze", "bandage", "antiseptic", "gloves" },
                new[] { "gauze", SlotCropRecord.EmptyLabel, "antiseptic", "gloves" },
                99);
        }

        [Test]
        public void CreateRecords_OneRecordPerSlot_InSlotOrder()
        {
            List<SlotCropRecord> records = LabelCsv.CreateRecords(MissingCase());

            Assert.AreEqual(4, records.Count);
            for (int i = 0; i < records.Count; i++)
            {
                Assert.AreEqual(i + 1, records[i].SlotNumber);
            }
        }

        [Test]
        public void Record_LabelIsActualContent_ExpectedIsRecipe()
        {
            SlotCropRecord emptied = LabelCsv.CreateRecords(MissingCase())[1];

            Assert.AreEqual("bandage", emptied.ExpectedComponentId);
            Assert.AreEqual(SlotCropRecord.EmptyLabel, emptied.Label);
            Assert.AreEqual(BoxVariant.Missing, emptied.BoxVariant);
        }

        [Test]
        public void FormatRow_HasSameColumnCountAsHeader()
        {
            string row = LabelCsv.FormatRow(LabelCsv.CreateRecords(MissingCase())[1]);

            Assert.AreEqual(LabelCsv.Header.Split(',').Length, row.Split(',').Length);
            Assert.AreEqual("train/case_00012_slot2.png,train,12,EM,2,bandage,empty,missing", row);
        }

        [Test]
        public void FormatRow_WrongItem_UsesSnakeCaseVariant()
        {
            var boxCase = new SyntheticBoxCase(
                0, DataSplit.Test, "DX", BoxVariant.WrongItem, 1,
                new[] { "swab", "buffer_vial" },
                new[] { "needle", "buffer_vial" },
                1);

            string row = LabelCsv.FormatRow(LabelCsv.CreateRecords(boxCase)[0]);

            Assert.AreEqual("test/case_00000_slot1.png,test,0,DX,1,swab,needle,wrong_item", row);
        }

        [Test]
        public void FormatRow_EscapesCommaAndQuote()
        {
            var boxCase = new SyntheticBoxCase(
                1, DataSplit.Validation, "EM", BoxVariant.Normal, 0,
                new[] { "a,b" }, new[] { "say \"hi\"" }, 1);

            string row = LabelCsv.FormatRow(LabelCsv.CreateRecords(boxCase)[0]);

            StringAssert.Contains("\"a,b\"", row);
            StringAssert.Contains("\"say \"\"hi\"\"\"", row);
        }

        [Test]
        public void RelativePath_UsesLowerCaseSplitAndPaddedIndex()
        {
            Assert.AreEqual("validation/case_00007_slot4.png", SlotCropNaming.RelativePath(DataSplit.Validation, 7, 4));
        }
    }
}
