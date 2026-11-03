using System.Collections.Generic;

namespace MediKitTwin.ML
{
    /// <summary>생성된 데이터셋 폴더를 검사한 결과. Errors가 비어 있으면 통과이다. (TC-08)</summary>
    public sealed class DatasetVerification
    {
        private readonly List<string> _errors = new List<string>();
        private readonly Dictionary<DataSplit, int> _boxCounts = new Dictionary<DataSplit, int>();
        private readonly Dictionary<DataSplit, int> _normalBoxCounts = new Dictionary<DataSplit, int>();

        public IReadOnlyList<string> Errors => _errors;

        public bool IsValid => _errors.Count == 0;

        /// <summary>labels.csv의 데이터 행 수(크롭 한 장당 한 행).</summary>
        public int RowCount { get; internal set; }

        /// <summary>데이터셋 폴더 안의 PNG 파일 수.</summary>
        public int ImageCount { get; internal set; }

        public int GetBoxCount(DataSplit split) => _boxCounts.TryGetValue(split, out int n) ? n : 0;

        public int GetNormalBoxCount(DataSplit split) => _normalBoxCounts.TryGetValue(split, out int n) ? n : 0;

        public int GetDefectBoxCount(DataSplit split) => GetBoxCount(split) - GetNormalBoxCount(split);

        internal void AddError(string message) => _errors.Add(message);

        internal void CountBox(DataSplit split, bool isNormal)
        {
            _boxCounts[split] = GetBoxCount(split) + 1;
            if (isNormal)
            {
                _normalBoxCounts[split] = GetNormalBoxCount(split) + 1;
            }
        }
    }
}
