namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 서 보고 닿는 도구 방향을 찾은 한 번의 시도 결과. 코루틴은 값을 돌려줄 수 없으므로 호출하는 쪽이 넘기고 끝난 뒤 읽는다.
    /// 베이스가 자리에 가지 못한 경우(IsBaseStuck)와, 갔지만 닿는 방향이 없는 경우를 구분한다.
    /// </summary>
    public sealed class StandAttempt
    {
        /// <summary>베이스가 서는 자리(또는 경유지)에 도착하지 못했다. 자리를 바꿔도 소용없으므로 곧바로 끝낸다.</summary>
        public bool IsBaseStuck { get; private set; }

        /// <summary>서서 찾은 결과. 베이스가 못 갔으면 못 찾은 상태다.</summary>
        public OrientationSearchResult Search { get; private set; } = new OrientationSearchResult();

        public bool IsFound => !IsBaseStuck && Search.IsFound;

        public void MarkBaseStuck()
        {
            IsBaseStuck = true;
        }

        public void RecordSearch(OrientationSearchResult search)
        {
            Search = search;
        }
    }
}
