using System.Collections.Generic;
using System.Text;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// CSV 한 줄을 필드로 나눈다. 의미(순번, 수량 등)는 모르고 글자만 나누므로 다른 CSV 입력에도 쓸 수 있다.
    /// 병원명에 쉼표가 들어갈 수 있어 따옴표로 감싼 필드를 지원한다.
    /// </summary>
    public sealed class CsvLineSplitter
    {
        private readonly char _delimiter;
        private readonly char _quote;

        public CsvLineSplitter(char delimiter, char quote)
        {
            _delimiter = delimiter;
            _quote = quote;
        }

        public List<string> Split(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            bool isInQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == _quote)
                {
                    if (isInQuotes && i + 1 < line.Length && line[i + 1] == _quote)
                    {
                        current.Append(_quote);
                        i++;
                    }
                    else
                    {
                        isInQuotes = !isInQuotes;
                    }
                }
                else if (c == _delimiter && !isInQuotes)
                {
                    fields.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            fields.Add(current.ToString().Trim());
            return fields;
        }
    }
}
