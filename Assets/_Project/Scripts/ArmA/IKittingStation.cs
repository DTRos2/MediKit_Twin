using MediKitTwin.Common;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 키팅을 하는 설비를 밖에서 읽는 접점. 화면과 대시보드가 이 인터페이스만 보고 KittingController는 모른다.
    /// </summary>
    public interface IKittingStation
    {
        /// <summary>키팅 현황(만드는 중인 키트, 항목별 단계, 결과 횟수).</summary>
        IKittingStatus Status { get; }

        /// <summary>설비 상태(대기, 가동, 정지).</summary>
        StationState State { get; }
    }
}
