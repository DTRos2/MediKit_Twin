namespace MediKitTwin.Common
{
    /// <summary>
    /// 자동으로 돌아가는 설비를 하던 동작 중간에 바로 얼렸다가 그 자리에서 이어 가게 하는 접점. 설비 하나만 멈추고 나머지(다른 팔)는 계속 돌려야 할 때 쓴다.
    /// 키트 사이에서만 멈추는 IStationPause와 달리 하던 키트도 멈춘다.
    /// </summary>
    public interface IStationFreeze
    {
        /// <summary>하던 동작을 지금 멈춘다. 이미 멈춰 있으면 아무 일도 없다.</summary>
        void Freeze();

        /// <summary>멈춘 자리에서 이어 간다.</summary>
        void Unfreeze();

        bool IsFrozen { get; }
    }
}
