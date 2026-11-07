using System.Collections.Generic;
using MediKitTwin.ArmA;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 손가락 충돌체 하나가 무엇과 얼마나 깊이 겹쳐 있는지 글로 만든다. 읽기만 한다.
    /// 쥔 뒤 손가락이 목표보다 더 닫힐 때 그것이 쥔 물건 때문인지 다른 충돌체에 밀려서인지 가르는 데 쓴다.
    /// 볼록하지 않은 메시는 겹침 깊이를 계산할 수 없어 깊이 없이 이름만 적거나 건너뛴다.
    /// </summary>
    public sealed class FingerContactReporter
    {
        private readonly Graspable _item;
        private readonly float _minReportedOverlapM;

        public FingerContactReporter(Graspable item, float minReportedOverlapM)
        {
            _item = item;
            _minReportedOverlapM = minReportedOverlapM;
        }

        /// <summary>물건 상자와 이 손가락 충돌체가 겹쳐 있으면 겹친 깊이(mm)를 붙인 글. 겹치지 않거나 계산할 수 없으면 빈 글.</summary>
        public string Penetration(Collider finger)
        {
            Collider box = _item.GetComponent<BoxCollider>();
            if (box == null || !CanMeasure(finger))
            {
                return string.Empty;
            }

            bool overlaps = Physics.ComputePenetration(
                box, box.transform.position, box.transform.rotation,
                finger, finger.transform.position, finger.transform.rotation,
                out Vector3 _, out float distance);
            return overlaps ? $" · 물건과 겹침 {distance * 1000f:0.0}mm" : string.Empty;
        }

        /// <summary>이 손가락 충돌체와 겹친 다른 충돌체(자기 자신과 물건 제외)를 이름과 깊이(mm)로 적은 글. 없으면 빈 글.</summary>
        public string OtherOverlaps(Collider finger)
        {
            Bounds bounds = finger.bounds;
            Collider[] hits = Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            var parts = new List<string>();

            foreach (Collider other in hits)
            {
                if (IsSelfOrItem(finger, other))
                {
                    continue;
                }

                if (!CanMeasure(finger) || !CanMeasure(other))
                {
                    parts.Add($"{NameOf(other)}(깊이 미상)");
                    continue;
                }

                bool overlaps = Physics.ComputePenetration(
                    finger, finger.transform.position, finger.transform.rotation,
                    other, other.transform.position, other.transform.rotation,
                    out Vector3 _, out float distance);
                if (overlaps && distance > _minReportedOverlapM)
                {
                    parts.Add($"{NameOf(other)} {distance * 1000f:0.0}mm");
                }
            }

            return parts.Count == 0 ? string.Empty : $" · 겹친 것: {string.Join(", ", parts)}";
        }

        private bool IsSelfOrItem(Collider finger, Collider other)
        {
            return other == finger || other.transform.IsChildOf(_item.transform);
        }

        // 볼록하지 않은 메시는 Physics.ComputePenetration이 지원하지 않는다.
        private static bool CanMeasure(Collider collider)
        {
            return !(collider is MeshCollider mesh && !mesh.convex);
        }

        private static string NameOf(Collider collider)
        {
            return $"{collider.transform.parent?.name}/{collider.name}";
        }
    }
}
