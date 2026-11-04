using System;
using System.Collections.Generic;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 화면에 보여 줄 로그 줄 목록을 만들고 간직한다. 줄마다 분류 이름표를 구하는 일이 있으므로,
    /// 로그가 바뀌었거나 보기 조건(최소 등급, 줄 수)이 바뀐 때만 다시 만든다. 보여 주는 쪽은 바뀐 때만 화면을 고치면 된다.
    /// Unity 없이 시험할 수 있다.
    /// </summary>
    public sealed class LogRowList
    {
        private readonly LogTagClassifier _classifier;
        private readonly List<LogRowEntry> _rows = new List<LogRowEntry>();

        private LogBuffer _buffer;
        private int _version = -1;
        private LogSeverity _minimum;
        private int _maxLines;

        public LogRowList(LogTagClassifier classifier)
        {
            _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        }

        /// <summary>오래된 줄부터 순서대로.</summary>
        public IReadOnlyList<LogRowEntry> Rows => _rows;

        /// <summary>목록이 바뀌었으면 true. 새 줄이 와서 맨 아래로 내려가야 하는지 알아내는 데 쓴다.</summary>
        public bool Refresh(LogBuffer buffer, LogSeverity minimum, int maxLines)
        {
            bool isUnchanged = ReferenceEquals(buffer, _buffer)
                               && buffer.Version == _version
                               && minimum == _minimum
                               && maxLines == _maxLines;
            if (isUnchanged)
            {
                return false;
            }

            _rows.Clear();
            foreach (LogLine line in buffer.TailLines(minimum, maxLines))
            {
                LogTag tag = _classifier.Classify(line.Severity, line.Text);
                _rows.Add(new LogRowEntry(line, tag));
            }

            _buffer = buffer;
            _version = buffer.Version;
            _minimum = minimum;
            _maxLines = maxLines;
            return true;
        }
    }
}
