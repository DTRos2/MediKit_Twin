using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MediKitTwin.ArmB
{
    /// <summary>
    /// 팔이 목표로 못 가고 멈췄을 때, 팔(과 쥔 물건)의 충돌체 곁에 어떤 다른 충돌체가 붙어 있는지 찾아 글로 적는다.
    /// 관절 목표는 나왔는데 관절이 한 도도 안 움직이면 구동이 모자란 게 아니라 무언가에 걸린 것이다.
    /// 그 무언가의 이름을 실패 기록에 남겨 원인을 좁힐 때 쓴다.
    /// </summary>
    public static class ContactBlockerFinder
    {
        // 한 번에 적는 최대 쌍 수. 베이스가 받침에 닿아 있는 것처럼 늘 닿는 쌍이 많아도 기록이 길어지지 않게 한다.
        private const int MaxPairs = 12;

        /// <param name="movers">팔 쪽 충돌체(움직이는 쪽).</param>
        /// <param name="ignore">닿아 있어도 걸림으로 보지 않을 충돌체(팔 자신, 쥔 물건).</param>
        /// <param name="marginM">이 거리 안이면 닿았다고 본다(m).</param>
        /// <returns>가까운 순서로 적은 쌍들. 없으면 빈 문자열.</returns>
        public static string Describe(IEnumerable<Collider> movers, ICollection<Collider> ignore, float marginM)
        {
            var pairs = new List<(float gap, string text)>();
            var seen = new HashSet<(Collider, Collider)>();

            foreach (Collider mover in movers)
            {
                if (mover == null || !mover.enabled || mover.isTrigger)
                {
                    continue;
                }

                Bounds bounds = mover.bounds;
                Collider[] candidates = Physics.OverlapBox(bounds.center, bounds.extents + Vector3.one * marginM, Quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                foreach (Collider other in candidates)
                {
                    if (ignore.Contains(other) || !seen.Add((mover, other)))
                    {
                        continue;
                    }

                    float gap = GapBetween(mover, other);
                    if (gap <= marginM)
                    {
                        string gapText = gap < 0f ? "간격 미상" : $"간격 {gap * 1000f:0.0}mm";
                        pairs.Add((gap, $"\n  {mover.name} ↔ {PathOf(other.transform)} ({gapText})"));
                    }
                }
            }

            if (pairs.Count == 0)
            {
                return string.Empty;
            }

            pairs.Sort((a, b) => a.gap.CompareTo(b.gap));
            var text = new StringBuilder("\n팔 곁에 붙은 충돌체:");
            for (int i = 0; i < pairs.Count && i < MaxPairs; i++)
            {
                text.Append(pairs[i].text);
            }

            return text.ToString();
        }

        // 두 충돌체 사이 가장 가까운 거리(m). 겹치면 0. 볼록하지 않은 메시는 가까운 점을 구할 수 없어 -1(미상)을 돌려준다.
        private static float GapBetween(Collider a, Collider b)
        {
            if (!SupportsClosestPoint(a) || !SupportsClosestPoint(b))
            {
                return -1f;
            }

            Vector3 onB = b.ClosestPoint(a.bounds.center);
            Vector3 onA = a.ClosestPoint(onB);
            return Vector3.Distance(onA, b.ClosestPoint(onA));
        }

        private static bool SupportsClosestPoint(Collider collider)
        {
            return !(collider is MeshCollider mesh) || mesh.convex;
        }

        // 위로 올라가며 이름을 이어 어느 설비의 어느 부품인지 알 수 있게 한다(최대 3단계).
        private static string PathOf(Transform transform)
        {
            const int Levels = 3;
            var parts = new List<string>();
            for (int i = 0; i < Levels && transform != null; i++, transform = transform.parent)
            {
                parts.Insert(0, transform.name);
            }

            return string.Join("/", parts);
        }
    }
}
