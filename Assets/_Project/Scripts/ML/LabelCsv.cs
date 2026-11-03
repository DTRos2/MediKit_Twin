using System.Collections.Generic;
using System.Text;

namespace MediKitTwin.ML
{
    /// <summary>labels.csv를 만든다. 크롭 한 장당 한 행이다. (FR-08)</summary>
    public static class LabelCsv
    {
        public const string Header = "image_path,split,case_index,kit_code,slot_number,expected_component,label,box_variant";

        /// <summary>박스 하나의 슬롯마다 크롭 기록을 만든다. 슬롯 번호 순서다.</summary>
        public static List<SlotCropRecord> CreateRecords(SyntheticBoxCase boxCase)
        {
            var records = new List<SlotCropRecord>(boxCase.SlotCount);
            for (int slot = 1; slot <= boxCase.SlotCount; slot++)
            {
                string path = SlotCropNaming.RelativePath(boxCase.Split, boxCase.Index, slot);
                records.Add(new SlotCropRecord(path, boxCase, slot));
            }

            return records;
        }

        public static string FormatRow(SlotCropRecord record)
        {
            var sb = new StringBuilder();
            sb.Append(Escape(record.ImagePath)).Append(',');
            sb.Append(record.Split.ToString().ToLowerInvariant()).Append(',');
            sb.Append(record.CaseIndex).Append(',');
            sb.Append(Escape(record.KitCode)).Append(',');
            sb.Append(record.SlotNumber).Append(',');
            sb.Append(Escape(record.ExpectedComponentId)).Append(',');
            sb.Append(Escape(record.Label)).Append(',');
            sb.Append(ToSnakeCase(record.BoxVariant));
            return sb.ToString();
        }

        public static string ToSnakeCase(BoxVariant variant)
        {
            switch (variant)
            {
                case BoxVariant.Normal: return "normal";
                case BoxVariant.Missing: return "missing";
                default: return "wrong_item";
            }
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
