namespace MediKitTwin.Common
{
    /// <summary>
    /// 자동으로 돌아가는 설비를 키트 사이에서 멈췄다가 다시 돌리는 접점. 사람이 로봇을 직접 쓰는 동안(수동 키팅) 자동 동작과 겹치지 않게 한다.
    /// 하던 키트는 끝까지 만든 뒤에 멈춘다.
    /// </summary>
    public interface IStationPause
    {
        /// <summary>하던 키트를 마치면 멈추도록 요청한다.</summary>
        void RequestPause();

        /// <summary>멈춤 요청을 거두고 자동 동작을 다시 시작한다.</summary>
        void Resume();

        /// <summary>자동 동작이 실제로 멈춰 있으면 true. RequestPause 직후에는 하던 키트가 끝날 때까지 false이다.</summary>
        bool IsPaused { get; }
    }
}
