using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 알려 준 집는 자세들의 모음. 칸 ID마다 자세 하나를 두고, 같은 칸을 다시 저장하면 바꿔 끼운다.
    /// JSON 글자로 바꾸고 되돌린다(어디에 쓸지는 IPickPoseStorage가 맡는다). 씬 없이 시험한다.
    /// </summary>
    [Serializable]
    public sealed class PickPoseBook
    {
        [SerializeField] private List<TaughtPickPose> _poses = new List<TaughtPickPose>();

        public int Count => _poses.Count;

        /// <summary>저장된 칸 ID들을 이름순으로 돌려준다.</summary>
        public IReadOnlyList<string> SlotIds => _poses.Select(pose => pose.SlotId).OrderBy(id => id, StringComparer.Ordinal).ToList();

        /// <summary>같은 칸의 자세가 있으면 바꾸고, 없으면 추가한다.</summary>
        public void Set(TaughtPickPose pose)
        {
            if (pose == null)
            {
                throw new ArgumentNullException(nameof(pose));
            }

            _poses.RemoveAll(existing => existing.SlotId == pose.SlotId);
            _poses.Add(pose);
        }

        public bool TryGet(string slotId, out TaughtPickPose pose)
        {
            string key = PickSlotKey.Normalize(slotId);
            pose = _poses.FirstOrDefault(existing => existing.SlotId == key);
            return pose != null;
        }

        /// <summary>그 칸의 자세를 지운다. 없었으면 false.</summary>
        public bool Remove(string slotId)
        {
            string key = PickSlotKey.Normalize(slotId);
            return _poses.RemoveAll(existing => existing.SlotId == key) > 0;
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>글자에서 모음을 만든다. 비어 있으면 빈 모음이다. 형식이 틀리면 ArgumentException이 난다.</summary>
        public static PickPoseBook FromJson(string json)
        {
            var book = new PickPoseBook();
            if (!string.IsNullOrWhiteSpace(json))
            {
                JsonUtility.FromJsonOverwrite(json, book);
            }

            return book;
        }
    }
}
