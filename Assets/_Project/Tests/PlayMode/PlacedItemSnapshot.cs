using System.Collections.Generic;
using System.Text;
using MediKitTwin.ArmA;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 로봇이 집어서 내려놓은 물건들이 지금 어디에 어떻게 놓여 있는지 적는다. 키팅 현황의 "Placed"가 말뿐이 아니라
    /// 물건이 실제로 박스 아래에 놓였는지 확인하려는 시험용 도구이며 읽기만 한다.
    /// 씬에는 시험용 큐브 같은 Graspable도 있어, 지정한 이름의 박스 아래에 들어 있는 Graspable만 놓인 물건으로 센다.
    /// </summary>
    public sealed class PlacedItemSnapshot
    {
        private readonly Graspable[] _all;
        private readonly List<Graspable> _placed = new List<Graspable>();
        private readonly Dictionary<Graspable, Transform> _boxOf = new Dictionary<Graspable, Transform>();

        /// <param name="boxName">물건이 놓이는 박스 오브젝트의 이름. 물건은 이 박스의 자식이 된다.</param>
        public PlacedItemSnapshot(string boxName)
        {
            _all = Object.FindObjectsByType<Graspable>();
            foreach (Graspable item in _all)
            {
                Transform box = FindBoxAbove(item, boxName);
                if (box != null)
                {
                    _placed.Add(item);
                    _boxOf[item] = box;
                }
            }
        }

        /// <summary>
        /// 박스 아래에 놓인 물건이 모두 실제로 박스 영역(바닥판과 벽이 감싸는 곳) 안에 있으면 true.
        /// 물건을 슬롯으로 옮겨 놓지 않고 물리로 내려앉히므로, 튕겨 나갔거나 바닥을 뚫은 물건은 부모만 박스일 뿐 영역 밖에 있다.
        /// </summary>
        public bool AllInsideBox
        {
            get
            {
                foreach (Graspable item in _placed)
                {
                    if (!BoxFootprint.Contains(_boxOf[item], item.Center))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>박스 아래에 들어 있는 물건 수.</summary>
        public int PlacedCount => _placed.Count;

        /// <summary>박스 아래에 있는 물건 중 아직 손에 쥐어 있는 것이 없으면 true.</summary>
        public bool NoneStillHeld
        {
            get
            {
                foreach (Graspable item in _placed)
                {
                    if (item.IsHeld)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public string Describe()
        {
            var text = new StringBuilder();
            foreach (Graspable item in _all)
            {
                Vector3 p = item.transform.position;
                string parent = item.transform.parent != null ? item.transform.parent.name : "(부모 없음)";
                string kind = _placed.Contains(item) ? "놓인 물건" : "박스/기타";
                string area = string.Empty;
                if (_boxOf.TryGetValue(item, out Transform box))
                {
                    Vector3 local = box.InverseTransformPoint(p);
                    area = $", 박스 영역 안 {BoxFootprint.Contains(box, item.Center)}, 박스 기준 ({local.x:0.000},{local.y:0.000},{local.z:0.000}), 바닥판 위 {FormatGap(box, item)}";
                }

                text.AppendLine($"  [{kind}] {item.name}: 부모 {parent}, 위치 ({p.x:0.000},{p.y:0.000},{p.z:0.000}){area}, 쥐고 있음 {item.IsHeld}, 키네마틱 {item.Body.isKinematic}");
            }

            return text.ToString();
        }

        // 물건 충돌체 밑면이 바닥판 윗면보다 몇 cm 위인지와 충돌체가 이루는 크기(가로×높이×세로, cm)를 적는다.
        // 0에 가까우면 바닥에 앉은 것이고, 크게 양수이면 허공에서 고정된 것이며, 음수이면 바닥판을 파고든 것이다.
        private static string FormatGap(Transform box, Graspable item)
        {
            if (!BoxFootprint.TryGetFloorTopY(box, out float floorTopY))
            {
                return "(바닥판을 알 수 없음)";
            }

            Bounds total = default;
            bool hasBounds = false;
            foreach (Collider collider in item.Colliders)
            {
                if (!hasBounds)
                {
                    total = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    total.Encapsulate(collider.bounds);
                }
            }

            if (!hasBounds)
            {
                return "(충돌체 없음)";
            }

            Vector3 size = total.size * 100f;
            return $"{(total.min.y - floorTopY) * 100f:0.0}cm 위(충돌체 {size.x:0.0}x{size.y:0.0}x{size.z:0.0}cm)";
        }

        // 물건의 조상 중 이름이 boxName인 박스를 찾는다. 없으면 null.
        private static Transform FindBoxAbove(Graspable item, string boxName)
        {
            for (Transform parent = item.transform.parent; parent != null; parent = parent.parent)
            {
                if (parent.name == boxName)
                {
                    return parent;
                }
            }

            return null;
        }
    }
}
