#if UNITY_EDITOR
using System.Collections.Generic;
using MediKitTwin.Label;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 뚜껑에 라벨이 붙는 순간을 적는다. 읽기만 한다. (FR-11)
    /// 하역 대역이 끝에 닿은 박스를 치우므로, 시험이 끝난 뒤에는 라벨을 볼 수 없다. 그래서 매 프레임 새 라벨을 찾아
    /// 어느 키트인지, 무엇이 적혔는지, 어디에 붙었는지, 언제 붙었는지를 처음 본 순간에 남겨 둔다.
    /// </summary>
    internal sealed class LabelRecorder
    {
        private readonly Dictionary<int, Record> _byKit = new Dictionary<int, Record>();

        /// <summary>라벨을 받은 키트 번호들.</summary>
        public IEnumerable<int> Kits => _byKit.Keys;

        public bool TryGet(int kitSequence, out Record record) => _byKit.TryGetValue(kitSequence, out record);

        public void Sample(float simSeconds)
        {
            foreach (KitLabelTag tag in Object.FindObjectsByType<KitLabelTag>())
            {
                if (tag.Kit != null && !_byKit.ContainsKey(tag.Kit.KitSequence))
                {
                    Transform parent = tag.transform.parent;
                    _byKit[tag.Kit.KitSequence] = new Record(simSeconds, tag.Lines, parent != null ? parent.name : string.Empty);
                }
            }
        }

        public string Describe()
        {
            var lines = new List<string>();
            foreach (KeyValuePair<int, Record> pair in _byKit)
            {
                lines.Add($"  키트 {pair.Key}: {pair.Value.Time:0.0}초, 붙은 곳 {pair.Value.ParentName}, 적힌 글 [{string.Join(" / ", pair.Value.Lines)}]");
            }

            return lines.Count == 0 ? "  라벨이 붙은 키트 없음" : string.Join("\n", lines);
        }

        internal readonly struct Record
        {
            public Record(float time, string[] lines, string parentName)
            {
                Time = time;
                Lines = lines;
                ParentName = parentName;
            }

            public float Time { get; }

            public string[] Lines { get; }

            public string ParentName { get; }
        }
    }
}
#endif
