using System;
using System.Collections.Generic;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 화면에 그릴 로그 줄 목록을 만들고 간직한다. 줄마다 분류 이름표와 줄 높이를 구하는 일은 글 크기를 재야 해서 비싸므로,
    /// 로그가 바뀌었거나 보기 조건(최소 등급, 줄 수, 폭)이 바뀐 때만 다시 만든다. 그리는 쪽은 매 이벤트마다 같은 목록을 읽는다.
    /// 글 높이 재는 방법을 주입받으므로 Unity 없이 시험할 수 있다.
    /// </summary>
    public sealed class LogRowList
    {
        private const float WidthTolerancePx = 0.5f;

        private readonly LogTagClassifier _classifier;
        private readonly Func<string, float, float> _heightOf;
        private readonly List<LogRowEntry> _rows = new List<LogRowEntry>();

        private LogBuffer _buffer;
        private int _version = -1;
        private LogSeverity _minimum;
        private int _maxLines;
        private float _width = -1f;

        /// <param name="heightOf">글과 줄 폭을 받아 줄 높이를 돌려준다.</param>
        public LogRowList(LogTagClassifier classifier, Func<string, float, float> heightOf)
        {
            _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
            _heightOf = heightOf ?? throw new ArgumentNullException(nameof(heightOf));
        }

        /// <summary>오래된 줄부터 순서대로.</summary>
        public IReadOnlyList<LogRowEntry> Rows => _rows;

        /// <summary>목록이 바뀌었으면 true. 새 줄이 와서 맨 아래로 내려가야 하는지 알아내는 데 쓴다.</summary>
        public bool Refresh(LogBuffer buffer, LogSeverity minimum, int maxLines, float rowWidth)
        {
            bool isUnchanged = ReferenceEquals(buffer, _buffer)
                               && buffer.Version == _version
                               && minimum == _minimum
                               && maxLines == _maxLines
                               && Math.Abs(rowWidth - _width) < WidthTolerancePx;
            if (isUnchanged)
            {
                return false;
            }

            _rows.Clear();
            foreach (LogLine line in buffer.TailLines(minimum, maxLines))
            {
                LogTag tag = _classifier.Classify(line.Severity, line.Text);
                _rows.Add(new LogRowEntry(line, tag, _heightOf(line.Text, rowWidth)));
            }

            _buffer = buffer;
            _version = buffer.Version;
            _minimum = minimum;
            _maxLines = maxLines;
            _width = rowWidth;
            return true;
        }
    }
}
