using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 알려 준 자세 근처에서 팔이 겹치지 않고 닿는 도구 방향과 그 방향의 관절 길을 찾은 결과.
    /// 코루틴은 값을 돌려줄 수 없으므로 호출하는 쪽이 넘기고 끝난 뒤 읽는다.
    /// </summary>
    public sealed class ClearGraspPlan
    {
        public bool IsFound { get; private set; }

        /// <summary>고른 도구 방향. 알려 준 방향 그대로면 그 방향이다.</summary>
        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        /// <summary>고른 방향의 접근·집기·후퇴 관절 각도.</summary>
        public TaughtJointRoute Route { get; private set; }

        /// <summary>알려 준 방향에서 벌어진 정도(도). 0이면 알려 준 방향이 그대로 겹치지 않았다.</summary>
        public float DeviationDeg { get; private set; }

        /// <summary>겹치지 않는 방향이 없어서 겹침을 감수한 경우(겹침을 허용하는 규칙일 때만).</summary>
        public bool HasCollisionRisk { get; private set; }

        /// <summary>따져 본 후보 수.</summary>
        public int TriedCount { get; private set; }

        /// <summary>알려 준 방향이 처음 겹친 곳의 설명("링크 ↔ 장애물(경로 몇 %)"). 겹치지 않았으면 빈 글자.</summary>
        public string FirstBlock { get; private set; } = string.Empty;

        public void Found(Quaternion rotation, TaughtJointRoute route, float deviationDeg, bool hasCollisionRisk, int triedCount)
        {
            IsFound = true;
            Rotation = rotation;
            Route = route;
            DeviationDeg = deviationDeg;
            HasCollisionRisk = hasCollisionRisk;
            TriedCount = triedCount;
        }

        public void NotFound(int triedCount)
        {
            IsFound = false;
            Route = null;
            HasCollisionRisk = false;
            TriedCount = triedCount;
        }

        public void RecordFirstBlock(string description)
        {
            if (FirstBlock.Length == 0)
            {
                FirstBlock = description ?? string.Empty;
            }
        }
    }
}
