#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using MediKitTwin.ArmA;
using MediKitTwin.Conveyor;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 컨베이어를 타는 동안 박스 안의 물건이 박스와 함께 움직이는지 잰다. 시작할 때 받침 기준 위치를 적어 두고,
    /// 프레임마다 지금 위치가 그 자리에서 얼마나 벗어났는지의 최댓값을 물건별로 남긴다. 읽기만 하며 씬은 건드리지 않는다.
    /// 물건이 박스의 자식이어도 물리로 움직이는 중이면 받침을 따라가지 못하므로, 고정(키네마틱) 여부도 함께 적는다.
    /// </summary>
    internal sealed class CarriedItemDrift
    {
        private readonly KitBoxCarrier _carrier;
        private readonly List<Entry> _entries = new List<Entry>();

        public CarriedItemDrift(KitBoxCarrier carrier)
        {
            _carrier = carrier;
            foreach (Graspable item in carrier.Box.GetComponentsInChildren<Graspable>())
            {
                _entries.Add(new Entry(item, carrier.transform.InverseTransformPoint(item.transform.position)));
            }
        }

        /// <summary>받침 아래에서 지켜보는 물건 수.</summary>
        public int Count => _entries.Count;

        /// <summary>물건 중 받침 기준 자리에서 가장 멀리 벗어난 거리(m).</summary>
        public float MaxDriftM
        {
            get
            {
                float max = 0f;
                foreach (Entry entry in _entries)
                {
                    max = Mathf.Max(max, entry.MaxDriftM);
                }

                return max;
            }
        }

        /// <summary>한 번이라도 고정되지 않은(물리로 움직이는) 상태였던 물건이 있으면 true.</summary>
        public bool AnyWasSimulated
        {
            get
            {
                foreach (Entry entry in _entries)
                {
                    if (entry.WasSimulated)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Sample()
        {
            foreach (Entry entry in _entries)
            {
                entry.Sample(_carrier.transform);
            }
        }

        public string Describe()
        {
            var text = new StringBuilder();
            foreach (Entry entry in _entries)
            {
                text.AppendLine(entry.Describe(_carrier.transform));
            }

            return text.ToString();
        }

        private sealed class Entry
        {
            private readonly Graspable _item;
            private readonly Vector3 _startLocal;

            public Entry(Graspable item, Vector3 startLocal)
            {
                _item = item;
                _startLocal = startLocal;
            }

            public float MaxDriftM { get; private set; }

            public bool WasSimulated { get; private set; }

            public void Sample(Transform carrier)
            {
                if (_item == null)
                {
                    return;
                }

                float drift = Vector3.Distance(carrier.InverseTransformPoint(_item.transform.position), _startLocal);
                MaxDriftM = Mathf.Max(MaxDriftM, drift);
                WasSimulated |= !_item.Body.isKinematic;
            }

            public string Describe(Transform carrier)
            {
                if (_item == null)
                {
                    return "  (사라진 물건)";
                }

                return $"  {_item.name}: 최대 벗어남 {MaxDriftM * 100f:0.0}cm, 지금 받침 기준 {carrier.InverseTransformPoint(_item.transform.position)}, " +
                       $"고정 {_item.Body.isKinematic}, 충돌 감지 {_item.Body.collisionDetectionMode}, 한 번이라도 물리로 움직임 {WasSimulated}, 부모 {_item.transform.parent.name}";
            }
        }
    }
}
#endif
