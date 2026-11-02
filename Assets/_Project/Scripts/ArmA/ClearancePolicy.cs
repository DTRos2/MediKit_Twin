using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 장애물과 겹치지 않는 도구 방향을 고를 때의 규칙. 닿는 방향 가운데 겹치지 않는 것을 우선하고,
    /// 그런 방향이 없을 때 겹침을 감수하고 가장 덜 무리한 방향으로 갈지(RequireClear가 false), 실패로 끝낼지(true)를 정한다.
    /// </summary>
    public sealed class ClearancePolicy
    {
        // 기본 값: 겹침을 감수하는 방향도 허용하고, 처음 닿는 방향을 찾은 뒤 30개 더 후보를 봐서 겹치지 않는 방향을 찾는다.
        public static readonly ClearancePolicy Default = new ClearancePolicy(false, 30);

        public ClearancePolicy(bool requireClear, int lookaheadCandidates)
        {
            RequireClear = requireClear;
            LookaheadCandidates = Mathf.Max(0, lookaheadCandidates);
        }

        /// <summary>겹치지 않는 방향이 없으면 실패로 끝낼지.</summary>
        public bool RequireClear { get; }

        /// <summary>겹치지 않는 방향을 찾느라, 닿는 방향을 처음 찾은 뒤에도 더 확인하는 후보 수. 시간 제한이 없으면 모두 본다(RequireClear).</summary>
        public int LookaheadCandidates { get; }
    }
}
