using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>도구 방향 탐색의 결과. 코루틴은 값을 돌려줄 수 없으므로 호출하는 쪽이 넘기고 끝난 뒤 읽는다.</summary>
    public sealed class OrientationSearchResult
    {
        public bool IsFound { get; private set; }

        /// <summary>닿는 도구 방향. 찾지 못했으면 단위 회전이다.</summary>
        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        /// <summary>찾은 방향이 기본 방향에서 벌어진 각도(도). 0이면 기본 방향이 그대로 닿았다.</summary>
        public float DeviationDeg { get; private set; }

        /// <summary>확인해 본 후보 수.</summary>
        public int TriedCount { get; private set; }

        public void Found(Quaternion rotation, float deviationDeg, int triedCount)
        {
            IsFound = true;
            Rotation = rotation;
            DeviationDeg = deviationDeg;
            TriedCount = triedCount;
        }

        public void NotFound(int triedCount)
        {
            IsFound = false;
            TriedCount = triedCount;
        }
    }
}
