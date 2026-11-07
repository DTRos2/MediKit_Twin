using System;
using System.Collections.Generic;
using System.Text;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 최근 로그 줄을 정해진 개수만 담아 두고(넘치면 오래된 줄부터 버린다), 한 덩어리 글로 내보낸다.
    /// 어디서 온 로그인지, 어떻게 보여 주는지는 모른다(Unity 없이 시험한다). 바뀔 때마다 Version이 올라가므로
    /// 그리는 쪽은 Version이 같으면 글을 다시 만들지 않아도 된다.
    /// </summary>
    public sealed class LogBuffer
    {
        private readonly int _capacity;
        private readonly List<LogLine> _lines = new List<LogLine>();

        /// <param name="capacity">담아 둘 최대 줄 수(1 이상).</param>
        public LogBuffer(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "담을 줄 수는 1 이상이어야 한다.");
            }

            _capacity = capacity;
        }

        public IReadOnlyList<LogLine> Lines => _lines;

        /// <summary>줄이 추가되거나 지워질 때마다 올라가는 번호.</summary>
        public int Version { get; private set; }

        public void Add(double seconds, LogSeverity severity, string text)
        {
            if (_lines.Count >= _capacity)
            {
                _lines.RemoveAt(0);
            }

            _lines.Add(new LogLine(seconds, severity, text));
            Version++;
        }

        public void Clear()
        {
            if (_lines.Count == 0)
            {
                return;
            }

            _lines.Clear();
            Version++;
        }

        /// <summary>minimum 이상인 줄만, 오래된 줄부터 한 줄씩 이어 붙인 글. 줄이 없으면 빈 글자다.</summary>
        public string ToText(LogSeverity minimum = LogSeverity.Info)
        {
            return Join(minimum, int.MaxValue);
        }

        /// <summary>minimum 이상인 줄 가운데 가장 최근 maxLines줄만 이어 붙인 글. 화면에 끝부분만 보여 줄 때 쓴다.</summary>
        public string TailText(LogSeverity minimum, int maxLines)
        {
            return Join(minimum, maxLines);
        }

        /// <summary>minimum 이상인 줄 가운데 가장 최근 maxLines줄을 오래된 줄부터 순서대로 돌려준다. 줄마다 따로 그리는 화면이 쓴다.</summary>
        public IReadOnlyList<LogLine> TailLines(LogSeverity minimum, int maxLines)
        {
            var picked = new List<LogLine>();
            for (int i = _lines.Count - 1; i >= 0 && picked.Count < maxLines; i--)
            {
                if (_lines[i].Severity >= minimum)
                {
                    picked.Add(_lines[i]);
                }
            }

            picked.Reverse();
            return picked;
        }

        /// <summary>minimum 이상인 줄의 개수.</summary>
        public int CountAtLeast(LogSeverity minimum)
        {
            int count = 0;
            foreach (LogLine line in _lines)
            {
                if (line.Severity >= minimum)
                {
                    count++;
                }
            }

            return count;
        }

        private string Join(LogSeverity minimum, int maxLines)
        {
            // 뒤에서부터 maxLines줄을 고른 뒤 원래 순서(오래된 줄 먼저)로 이어 붙인다.
            var picked = new List<LogLine>();
            for (int i = _lines.Count - 1; i >= 0 && picked.Count < maxLines; i--)
            {
                if (_lines[i].Severity >= minimum)
                {
                    picked.Add(_lines[i]);
                }
            }

            var text = new StringBuilder();
            for (int i = picked.Count - 1; i >= 0; i--)
            {
                if (text.Length > 0)
                {
                    text.Append('\n');
                }

                text.Append(picked[i]);
            }

            return text.ToString();
        }
    }
}
