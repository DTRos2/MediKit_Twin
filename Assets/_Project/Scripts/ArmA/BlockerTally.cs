using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 방향 후보가 팔이 닿는데도 버려진 까닭("링크 ↔ 장애물")을 같은 것끼리 세어 둔다. 후보를 수십 가지 버린 뒤에
    /// "어느 장애물이 가장 자주 길을 막았는지"를 한 줄로 보이려는 것이라, 막은 곳을 고치는 방향(서는 자리, 접근 방향, 박스 모양)을 정하는 근거가 된다.
    /// 같은 곳에서 겹쳐도 길의 몇 % 지점인지는 후보마다 달라 따로 세면 흩어지므로, 지점을 나타내는 괄호 뒤는 떼고 센다.
    /// </summary>
    public sealed class BlockerTally
    {
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        /// <summary>센 서로 다른 막힘의 종류 수.</summary>
        public int KindCount => _counts.Count;

        /// <summary>겹침 설명 하나를 센다. 빈 글자는 겹치지 않았거나 설명할 수 없는 것이므로 세지 않는다.</summary>
        public void Record(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                return;
            }

            string key = WithoutPosition(description);
            _counts.TryGetValue(key, out int count);
            _counts[key] = count + 1;
        }

        /// <summary>
        /// 가장 자주 막은 곳부터 maxKinds개를 "링크 ↔ 장애물 n번, …"으로 적는다. 센 것이 없으면 빈 글자.
        /// 횟수가 같으면 이름 순으로 두어 같은 입력이 늘 같은 글을 낳게 한다.
        /// </summary>
        public string Describe(int maxKinds)
        {
            if (_counts.Count == 0)
            {
                return string.Empty;
            }

            var text = new StringBuilder();
            foreach (KeyValuePair<string, int> entry in _counts.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).Take(maxKinds))
            {
                text.Append(text.Length == 0 ? string.Empty : ", ").Append(entry.Key).Append(' ').Append(entry.Value).Append('번');
            }

            int hidden = _counts.Count - maxKinds;
            if (hidden > 0)
            {
                text.Append(", 그 밖에 ").Append(hidden).Append("곳");
            }

            return text.ToString();
        }

        private static string WithoutPosition(string description)
        {
            int open = description.IndexOf('(');
            return open > 0 ? description.Substring(0, open).TrimEnd() : description;
        }
    }
}
