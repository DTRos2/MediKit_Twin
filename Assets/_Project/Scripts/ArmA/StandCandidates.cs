using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 서 볼 자리 후보를 만든다. 물러나는 거리 목록과 옆으로 비키는 거리 목록의 모든 조합을 만들고, 기본 자리에 가까운 순서로 늘어놓는다.
    /// 자리의 실제 좌표는 호출하는 쪽이 정하므로(선반 방향, 박스 방향 등) 이 클래스는 조합과 순서만 맡는다. 순수 계산이라 씬 없이 시험한다.
    /// </summary>
    public static class StandCandidates
    {
        /// <param name="standoffExtrasM">기본 거리에 더해 볼 거리들(m).</param>
        /// <param name="lateralsM">옆으로 비켜 볼 거리들(m).</param>
        /// <param name="standFor">(물러나는 거리, 비키는 거리)에 해당하는 팔 베이스 지점.</param>
        public static IReadOnlyList<StandCandidate> Generate(
            IReadOnlyList<float> standoffExtrasM, IReadOnlyList<float> lateralsM, Func<float, float, Vector3> standFor)
        {
            if (standoffExtrasM == null || lateralsM == null || standFor == null)
            {
                throw new ArgumentNullException(standoffExtrasM == null ? nameof(standoffExtrasM) : lateralsM == null ? nameof(lateralsM) : nameof(standFor));
            }

            var candidates = new List<StandCandidate>(standoffExtrasM.Count * lateralsM.Count);
            foreach (float extra in standoffExtrasM)
            {
                foreach (float lateral in lateralsM)
                {
                    candidates.Add(new StandCandidate(standFor(extra, lateral), extra, lateral));
                }
            }

            // 안정 정렬이라 벗어난 거리가 같으면 목록에 적은 순서를 지킨다.
            return candidates.OrderBy(candidate => candidate.ShiftM).ToList();
        }
    }
}
