using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 박스에 놓을 때 방향 찾기가 고른 것: 닿는 도구 방향, 놓는 높이, 그 높이가 놓는 높이 목록에서 몇 번째인지.
    /// 코루틴은 값을 돌려줄 수 없으므로 찾는 쪽이 이 객체를 채우고 부르는 쪽이 끝난 뒤 읽는다.
    /// 높이 순번을 남겨 두면, 고른 높이에서 내려가지 못했을 때 그보다 높은 높이부터 다시 찾을 수 있다.
    /// 고른 방식을 PlacementRecall로도 남겨, 내려가기까지 성공하면 기억해 두었다가 다음에 같은 슬롯에서 먼저 쓴다.
    /// </summary>
    internal sealed class PlacementChoice
    {
        /// <summary>찾은 방향. 못 찾았으면 IsFound가 false인 결과이다.</summary>
        public OrientationSearchResult Search { get; private set; } = new OrientationSearchResult();

        public bool IsFound => Search.IsFound;

        /// <summary>슬롯 중심 위 놓는 높이(m).</summary>
        public float ReleaseHeight { get; private set; }

        /// <summary>놓는 높이 목록에서 고른 높이의 순번. 못 찾았으면 -1이다.</summary>
        public int HeightIndex { get; private set; } = -1;

        /// <summary>방향을 찾을 때 도구 자리를 계산한 기준 방향.</summary>
        public Quaternion SearchedNominal { get; private set; } = Quaternion.identity;

        /// <summary>고른 방식. 기억해 두었다가 같은 슬롯에서 먼저 쓴다.</summary>
        public PlacementRecall Recall { get; private set; }

        /// <summary>처음부터 찾지 않고 기억해 둔 방식을 그대로 써서 닿은 것이면 true.</summary>
        public bool IsReplayed { get; private set; }

        /// <summary>못 찾았을 때 마지막으로 시도한 높이의 실패 이유.</summary>
        public TransferResult SearchFailure { get; private set; } = new TransferResult();

        public void SetFound(OrientationSearchResult search, float releaseHeight, int heightIndex, Quaternion searchedNominal, PlacementRecall recall)
        {
            Search = search;
            ReleaseHeight = releaseHeight;
            HeightIndex = heightIndex;
            SearchedNominal = searchedNominal;
            Recall = recall;
        }

        /// <summary>찾은 방식이 기억해 둔 방식을 그대로 쓴 것임을 표시한다.</summary>
        public void MarkReplayed()
        {
            IsReplayed = true;
        }

        public void SetFailure(TransferResult failure)
        {
            SearchFailure = failure;
        }
    }
}
