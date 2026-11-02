using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>도구 방향 탐색의 결과. 코루틴은 값을 돌려줄 수 없으므로 호출하는 쪽이 넘기고 끝난 뒤 읽는다.</summary>
    public sealed class OrientationSearchResult
    {
        public bool IsFound { get; private set; }

        /// <summary>닿는 도구 방향. 찾지 못했으면 단위 회전이다.</summary>
        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        /// <summary>찾은 방향이 기본 방향에서 벌어진 정도(도). 손목 비틀기는 가중치만큼 줄여 센다. 0이면 기본 방향이 그대로 닿았다.</summary>
        public float DeviationDeg { get; private set; }

        /// <summary>기본 방향에서 기울인 각도(도). 도구 끝이 아래로 향하면 양수.</summary>
        public float PitchDeg { get; private set; }

        /// <summary>기본 방향에서 좌우로 돌린 각도(도).</summary>
        public float YawDeg { get; private set; }

        /// <summary>기본 방향에서 손목을 비튼 각도(도).</summary>
        public float RollDeg { get; private set; }

        /// <summary>확인해 본 후보 수.</summary>
        public int TriedCount { get; private set; }

        /// <summary>확인한 후보 가운데 팔이 닿지 않은 수. 못 찾은 이유가 도달인지 장애물인지 가를 때 쓴다.</summary>
        public int UnreachableCount { get; private set; }

        /// <summary>확인한 후보 가운데 팔은 닿지만 장애물과 겹쳐 쓸 수 없던 수.</summary>
        public int BlockedCount { get; private set; }

        /// <summary>확인한 후보가 닿지 않았던 수와 닿지만 겹쳤던 수를 남긴다.</summary>
        public void RecordVerdicts(int unreachable, int blocked)
        {
            UnreachableCount = unreachable;
            BlockedCount = blocked;
        }

        /// <summary>닿지만 겹쳐 버려진 후보들이 어떤 링크와 장애물에서 겹쳤는지의 집계. 겹침을 설명하는 쪽이 없으면 비어 있다.</summary>
        public BlockerTally Blockers { get; } = new BlockerTally();

        // 로그 한 줄에 다 싣기엔 길어서 가장 자주 막은 곳만 보인다.
        private const int ShownBlockerKinds = 4;

        /// <summary>"닿지 않음 a가지, 닿지만 겹침 b가지"처럼 못 찾은 이유를 가르는 글. 겹친 곳을 셌다면 가장 자주 겹친 곳도 덧붙인다.</summary>
        public string DescribeVerdicts()
        {
            string verdicts = $"닿지 않음 {UnreachableCount}가지, 닿지만 장애물과 겹침 {BlockedCount}가지";
            string blockers = Blockers.Describe(ShownBlockerKinds);
            return blockers.Length == 0 ? verdicts : $"{verdicts}. 가장 자주 겹친 곳: {blockers}";
        }

        /// <summary>찾은 방향이 닿기는 하지만 팔이 장애물과 겹칠 수 있으면 true. 겹치지 않는 방향이 없어서 감수한 경우다.</summary>
        public bool HasCollisionRisk { get; private set; }

        public void Found(OrientationCandidate candidate, int triedCount)
        {
            Found(candidate, triedCount, hasCollisionRisk: false);
        }

        public void Found(OrientationCandidate candidate, int triedCount, bool hasCollisionRisk)
        {
            Found(candidate.Rotation, candidate.DeviationDeg, triedCount);
            PitchDeg = candidate.PitchDeg;
            YawDeg = candidate.YawDeg;
            RollDeg = candidate.RollDeg;
            HasCollisionRisk = hasCollisionRisk;
        }

        public void Found(Quaternion rotation, float deviationDeg, int triedCount)
        {
            HasCollisionRisk = false;
            IsFound = true;
            Rotation = rotation;
            DeviationDeg = deviationDeg;
            PitchDeg = 0f;
            YawDeg = 0f;
            RollDeg = 0f;
            TriedCount = triedCount;
        }

        public void NotFound(int triedCount)
        {
            HasCollisionRisk = false;
            IsFound = false;
            TriedCount = triedCount;
        }
    }
}
