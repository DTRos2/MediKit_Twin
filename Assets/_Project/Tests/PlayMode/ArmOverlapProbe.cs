using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 지금 팔의 충돌체가 팔이 아닌 어떤 충돌체와 겹치는지 한 번 조회해 적는다. 접촉 알림(OnCollision*)을 켜지 않는 이유는
    /// 이 프로젝트에서 팔 링크에 접촉 알림을 켜면 PhysX 물리 단계에서 Unity가 죽기 때문이다. 조회만 하고 아무것도 바꾸지 않는다.
    /// </summary>
    public sealed class ArmOverlapProbe
    {
        private const float MarginM = 0.01f;

        private readonly Transform _robotRoot;

        public ArmOverlapProbe(Transform robotRoot)
        {
            _robotRoot = robotRoot;
        }

        public string Describe()
        {
            var text = new StringBuilder();
            var seen = new HashSet<string>();
            text.AppendLine($"  팔 충돌체 {_robotRoot.GetComponentsInChildren<Collider>().Length}개 조회");

            foreach ((Collider arm, Collider other) in NearbyPairs())
            {
                string line = Line(arm, other);
                if (seen.Add(line))
                {
                    text.AppendLine(line);
                }
            }

            return text.ToString();
        }

        /// <summary>
        /// 실제로 파고든 쌍만 "링크↔상대 깊이"로 한 줄에 줄여 적는다. 같은 쌍은 가장 깊은 값 하나만 남기고, 트리거는 막지 못하므로 뺀다.
        /// 단계마다 남겨 통과한 시험과 실패한 시험을 같은 모양으로 견주기 위한 것이다.
        /// </summary>
        public string Summarize()
        {
            var deepest = new SortedDictionary<string, float>();
            foreach ((Collider arm, Collider other) in NearbyPairs())
            {
                if (other.isTrigger || !TryMeasureDepth(arm, other, out float depth))
                {
                    continue;
                }

                string key = $"{OwnerLink(arm)}↔{other.name}";
                deepest[key] = deepest.TryGetValue(key, out float known) ? Mathf.Max(known, depth) : depth;
            }

            if (deepest.Count == 0)
            {
                return "없음";
            }

            var text = new StringBuilder();
            foreach (KeyValuePair<string, float> pair in deepest)
            {
                text.Append(text.Length == 0 ? string.Empty : ", ").Append($"{pair.Key} {pair.Value * 100f:0.0}cm");
            }

            return text.ToString();
        }

        // 팔 충돌체마다 범위가 겹치는 팔 밖의 충돌체를 찾는다. 범위만 겹치고 실제로는 안 겹치는 쌍도 들어 있다.
        private IEnumerable<(Collider arm, Collider other)> NearbyPairs()
        {
            foreach (Collider arm in _robotRoot.GetComponentsInChildren<Collider>())
            {
                if (!arm.enabled)
                {
                    continue;
                }

                Bounds box = arm.bounds;
                Collider[] near = Physics.OverlapBox(box.center, box.extents + Vector3.one * MarginM, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
                foreach (Collider other in near)
                {
                    if (other.transform.IsChildOf(_robotRoot) || other.transform == _robotRoot)
                    {
                        continue;
                    }

                    yield return (arm, other);
                }
            }
        }

        private static string Line(Collider arm, Collider other)
        {
            string depth = Penetration(arm, other);
            string layer = $"{LayerMask.LayerToName(other.gameObject.layer)}/{other.gameObject.layer}";
            return $"  {arm.name}(소속 링크 {OwnerLink(arm)}, {arm.GetType().Name}, 레이어 {arm.gameObject.layer}, 아래끝 y={arm.bounds.min.y:0.000}) ↔ " +
                   $"{other.name}({other.GetType().Name}, 루트 {other.transform.root.name}, 레이어 {layer}{(other.isTrigger ? ", 트리거" : string.Empty)}, " +
                   $"윗면 y={other.bounds.max.y:0.000}, 중심 {Format(other.bounds.center)}, 크기 {Format(other.bounds.size)}, 칸 위치 {Path(other.transform)}) {depth}";
        }

        private static string Format(Vector3 v) => $"({v.x:0.00},{v.y:0.00},{v.z:0.00})";

        // 링크 메시 이름은 여러 링크가 같이 쓰므로(FR5는 모두 같은 이름), 어느 링크의 충돌체인지는 가장 가까운 관절 몸체 이름으로 알린다.
        private static string OwnerLink(Collider collider)
        {
            ArticulationBody body = collider.GetComponentInParent<ArticulationBody>();
            return body != null ? body.name : "없음";
        }

        private static string Path(Transform t)
        {
            string path = t.name;
            for (Transform parent = t.parent; parent != null; parent = parent.parent)
            {
                path = parent.name + "/" + path;
            }

            return path;
        }

        // 겹침 깊이는 모양이 허락하는 쌍(상자·구·캡슐·볼록 메시)만 잴 수 있다. 못 재면 상자 범위가 겹친다는 사실만 적는다.
        private static string Penetration(Collider a, Collider b)
        {
            if (!CanMeasure(a) || !CanMeasure(b))
            {
                return "(범위만 겹침)";
            }

            bool overlaps = Physics.ComputePenetration(
                a, a.transform.position, a.transform.rotation, b, b.transform.position, b.transform.rotation, out Vector3 direction, out float distance);
            // direction은 팔 충돌체를 떼어 놓으려면 밀어야 하는 방향이다. 회전하는 링크가 어느 쪽으로 눌려 있는지 가린다.
            return overlaps ? $"겹침 깊이 {distance * 100f:0.0}cm, 팔을 떼어 놓는 방향 {Format(direction)}" : "(실제로는 안 겹침)";
        }

        private static bool TryMeasureDepth(Collider a, Collider b, out float depth)
        {
            depth = 0f;
            if (!CanMeasure(a) || !CanMeasure(b))
            {
                return false;
            }

            return Physics.ComputePenetration(
                a, a.transform.position, a.transform.rotation, b, b.transform.position, b.transform.rotation, out Vector3 _, out depth);
        }

        private static bool CanMeasure(Collider collider)
        {
            return !(collider is MeshCollider mesh) || mesh.convex;
        }
    }
}
