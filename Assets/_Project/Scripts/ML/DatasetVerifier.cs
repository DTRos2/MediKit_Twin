using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediKitTwin.ML
{
    /// <summary>
    /// 생성된 데이터셋 폴더가 FR-08의 인수 기준을 지키는지 검사한다. (TC-08)
    /// - labels.csv 행 수와 이미지(PNG) 수가 같고, 행마다 이미지 파일이 있다.
    /// - 테스트 세트의 정상 박스와 불량 박스 수가 같다.
    /// - 박스마다 라벨이 박스 종류와 맞다: 정상은 레시피와 같고, 누락은 빈 슬롯 1칸, 오구성은 다른 구성품 1칸이다.
    /// </summary>
    public static class DatasetVerifier
    {
        private const int ColumnCount = 8;

        public static DatasetVerification Verify(string datasetDir)
        {
            var result = new DatasetVerification();
            string csvPath = Path.Combine(datasetDir, "labels.csv");
            if (!File.Exists(csvPath))
            {
                result.AddError("labels.csv가 없다.");
                return result;
            }

            string[] lines = File.ReadAllLines(csvPath).Where(l => l.Length > 0).ToArray();
            if (lines.Length == 0 || lines[0] != LabelCsv.Header)
            {
                result.AddError("labels.csv의 머리글이 규칙과 다르다.");
                return result;
            }

            var rows = new List<string[]>();
            for (int i = 1; i < lines.Length; i++)
            {
                string[] cells = SplitCsvLine(lines[i]);
                if (cells.Length != ColumnCount)
                {
                    result.AddError($"{i + 1}번째 줄의 열 수가 {cells.Length}개이다. ({ColumnCount}개여야 한다)");
                    continue;
                }

                rows.Add(cells);
            }

            result.RowCount = rows.Count;
            result.ImageCount = Directory.Exists(datasetDir)
                ? Directory.GetFiles(datasetDir, "*.png", SearchOption.AllDirectories).Length
                : 0;

            if (result.RowCount != result.ImageCount)
            {
                result.AddError($"labels.csv 행 수({result.RowCount})와 이미지 수({result.ImageCount})가 다르다.");
            }

            CheckImages(datasetDir, rows, result);
            CheckBoxes(rows, result);
            CheckTestBalance(result);
            return result;
        }

        private static void CheckImages(string datasetDir, List<string[]> rows, DatasetVerification result)
        {
            var seen = new HashSet<string>();
            foreach (string[] row in rows)
            {
                string path = row[0];
                if (!seen.Add(path))
                {
                    result.AddError($"이미지 경로가 중복된다: {path}");
                }

                if (!File.Exists(Path.Combine(datasetDir, path)))
                {
                    result.AddError($"이미지 파일이 없다: {path}");
                }
            }
        }

        private static void CheckBoxes(List<string[]> rows, DatasetVerification result)
        {
            foreach (var box in rows.GroupBy(r => (split: r[1], caseIndex: r[2])))
            {
                if (!Enum.TryParse(box.Key.split, true, out DataSplit split))
                {
                    result.AddError($"알 수 없는 분할이다: {box.Key.split}");
                    continue;
                }

                string label = $"{box.Key.split} 박스 {box.Key.caseIndex}";
                string variant = box.First()[7];
                if (box.Any(r => r[7] != variant))
                {
                    result.AddError($"{label}: 같은 박스의 행인데 박스 종류가 서로 다르다.");
                    continue;
                }

                int mismatched = box.Count(r => r[5] != r[6]);
                int empty = box.Count(r => r[6] == SlotCropRecord.EmptyLabel);
                string slotProblem = null;
                switch (variant)
                {
                    case "normal":
                        if (mismatched != 0) slotProblem = "정상인데 레시피와 다른 슬롯이 있다.";
                        break;
                    case "missing":
                        if (mismatched != 1 || empty != 1) slotProblem = "누락은 빈 슬롯이 정확히 1칸이어야 한다.";
                        break;
                    case "wrong_item":
                        if (mismatched != 1 || empty != 0) slotProblem = "오구성은 다른 구성품이 든 슬롯이 정확히 1칸이어야 한다.";
                        break;
                    default:
                        slotProblem = $"알 수 없는 박스 종류이다: {variant}";
                        break;
                }

                if (slotProblem != null)
                {
                    result.AddError($"{label}: {slotProblem}");
                }

                result.CountBox(split, variant == "normal");
            }
        }

        private static void CheckTestBalance(DatasetVerification result)
        {
            int normal = result.GetNormalBoxCount(DataSplit.Test);
            int defect = result.GetDefectBoxCount(DataSplit.Test);
            if (normal != defect)
            {
                result.AddError($"테스트 세트의 정상 박스({normal})와 불량 박스({defect})가 같지 않다.");
            }
        }

        // 따옴표로 감싼 값 안의 쉼표를 값으로 다룬다. LabelCsv.Escape가 만든 형식만 읽는다.
        private static string[] SplitCsvLine(string line)
        {
            var cells = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        inQuotes = false;
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    cells.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            cells.Add(current.ToString());
            return cells.ToArray();
        }
    }
}
