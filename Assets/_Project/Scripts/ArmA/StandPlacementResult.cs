using System.Collections.Generic;
using System.Linq;

namespace MediKitTwin.ArmA
{
    /// <summary>서는 자리 탐색의 결과. 코루틴은 값을 돌려줄 수 없으므로 호출하는 쪽이 넘기고 끝난 뒤 읽는다.</summary>
    public sealed class StandPlacementResult
    {
        private readonly List<StandPlacementOption> _options = new List<StandPlacementOption>();

        /// <summary>팔이 닿는 것으로 가늠된 자리들. 점수가 좋은(작은) 순서다. 점수가 같으면 찾은 순서를 지킨다.</summary>
        public IReadOnlyList<StandPlacementOption> Options => _options;

        public bool HasOptions => _options.Count > 0;

        /// <summary>실제로 닿는지 따져 본 자리 수.</summary>
        public int EvaluatedCount { get; private set; }

        public void Add(StandPlacementOption option)
        {
            _options.Add(option);
        }

        /// <summary>탐색을 마친다. 점수 순서로 정렬하고 따져 본 자리 수를 남긴다.</summary>
        public void Finish(int evaluatedCount)
        {
            EvaluatedCount = evaluatedCount;

            // OrderBy는 안정 정렬이라 점수가 같으면 먼저 찾은(기본 자리에 가까운) 쪽이 앞에 남는다.
            List<StandPlacementOption> ordered = _options.OrderBy(option => option.Cost).ToList();
            _options.Clear();
            _options.AddRange(ordered);
        }
    }
}
