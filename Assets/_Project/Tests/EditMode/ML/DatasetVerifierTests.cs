using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MediKitTwin.ML.Tests
{
    public sealed class DatasetVerifierTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "medikit_dataset_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        private static List<SyntheticKitSpec> Kits()
        {
            return new List<SyntheticKitSpec>
            {
                new SyntheticKitSpec("EM", new[] { "gauze", "bandage", "antiseptic", "gloves" }),
                new SyntheticKitSpec("BL", new[] { "blood_tube", "needle", "alcohol_pad", "tourniquet" }),
                new SyntheticKitSpec("DX", new[] { "swab", "buffer_vial", "test_cassette", "sample_bag" }),
            };
        }

        // 생성기와 같은 규칙(계획 → 크롭 기록 → 이미지 경로 → labels.csv)으로 데이터셋을 만든다. 이미지는 빈 파일이다.
        private List<SlotCropRecord> WriteDataset(int train, int validation, int test, bool writeImages = true)
        {
            var records = new List<SlotCropRecord>();
            var plans = new[]
            {
                (DataSplit.Train, train, 1),
                (DataSplit.Validation, validation, 2),
                (DataSplit.Test, test, 3),
            };

            foreach (var (split, count, seed) in plans)
            {
                foreach (SyntheticBoxCase boxCase in SyntheticCasePlanner.Plan(Kits(), split, count, seed))
                {
                    records.AddRange(LabelCsv.CreateRecords(boxCase));
                }
            }

            if (writeImages)
            {
                foreach (SlotCropRecord record in records)
                {
                    string path = Path.Combine(_dir, record.ImagePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllBytes(path, new byte[] { 1 });
                }
            }

            WriteCsv(records);
            return records;
        }

        private void WriteCsv(IEnumerable<SlotCropRecord> records)
        {
            var sb = new StringBuilder();
            sb.Append(LabelCsv.Header).Append('\n');
            foreach (SlotCropRecord record in records)
            {
                sb.Append(LabelCsv.FormatRow(record)).Append('\n');
            }

            File.WriteAllText(Path.Combine(_dir, "labels.csv"), sb.ToString());
        }

        [Test]
        public void GeneratedDataset_Passes_RowCountEqualsImageCount()
        {
            WriteDataset(train: 40, validation: 10, test: 20);

            DatasetVerification result = DatasetVerifier.Verify(_dir);

            Assert.IsTrue(result.IsValid, string.Join("\n", result.Errors));
            Assert.AreEqual(70 * 4, result.RowCount);
            Assert.AreEqual(result.RowCount, result.ImageCount);
        }

        [Test]
        public void GeneratedDataset_TestSetHasSameNormalAndDefectBoxes()
        {
            WriteDataset(train: 20, validation: 10, test: 30);

            DatasetVerification result = DatasetVerifier.Verify(_dir);

            Assert.AreEqual(30, result.GetBoxCount(DataSplit.Test));
            Assert.AreEqual(15, result.GetNormalBoxCount(DataSplit.Test));
            Assert.AreEqual(15, result.GetDefectBoxCount(DataSplit.Test));
        }

        [Test]
        public void MissingLabelsFile_Fails()
        {
            Assert.IsFalse(DatasetVerifier.Verify(_dir).IsValid);
        }

        [Test]
        public void ImageNotWritten_IsReported()
        {
            List<SlotCropRecord> records = WriteDataset(train: 10, validation: 0, test: 10);
            File.Delete(Path.Combine(_dir, records[5].ImagePath));

            DatasetVerification result = DatasetVerifier.Verify(_dir);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("이미지 파일이 없다")));
            Assert.IsTrue(result.Errors.Any(e => e.Contains("행 수")));
        }

        [Test]
        public void ExtraRowWithoutImage_IsReported()
        {
            List<SlotCropRecord> records = WriteDataset(train: 10, validation: 0, test: 10);
            WriteCsv(records.Concat(new[] { records[0] }));

            DatasetVerification result = DatasetVerifier.Verify(_dir);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("중복")));
        }

        [Test]
        public void TestSetWithUnequalNormalAndDefect_IsReported()
        {
            // 테스트 박스 20개 중 정상만 남기면 정상 10, 불량 0이 된다.
            List<SlotCropRecord> records = WriteDataset(train: 10, validation: 0, test: 20);
            WriteCsv(records.Where(r => r.Split != DataSplit.Test || r.BoxVariant == BoxVariant.Normal));

            DatasetVerification result = DatasetVerifier.Verify(_dir);

            Assert.IsTrue(result.Errors.Any(e => e.Contains("테스트 세트의 정상 박스")));
        }

        [Test]
        public void BoxWhoseLabelDoesNotMatchItsVariant_IsReported()
        {
            List<SlotCropRecord> records = WriteDataset(train: 20, validation: 0, test: 0);
            string row = LabelCsv.FormatRow(records.First(r => r.BoxVariant == BoxVariant.Missing && r.Label == SlotCropRecord.EmptyLabel));
            string tampered = row.Replace(",empty,", ",gauze,");
            string csv = File.ReadAllText(Path.Combine(_dir, "labels.csv")).Replace(row, tampered);
            File.WriteAllText(Path.Combine(_dir, "labels.csv"), csv);

            DatasetVerification result = DatasetVerifier.Verify(_dir);

            Assert.IsTrue(result.Errors.Any(e => e.Contains("누락은 빈 슬롯이 정확히 1칸")));
        }

        [Test]
        public void WrongColumnCount_IsReported()
        {
            WriteDataset(train: 4, validation: 0, test: 0);
            File.AppendAllText(Path.Combine(_dir, "labels.csv"), "a,b,c\n");

            Assert.IsTrue(DatasetVerifier.Verify(_dir).Errors.Any(e => e.Contains("열 수")));
        }
    }
}
