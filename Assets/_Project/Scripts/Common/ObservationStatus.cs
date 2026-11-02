namespace MediKitTwin.Common
{
    /// <summary>카메라로 물건을 찾아본 결과의 종류.</summary>
    public enum ObservationStatus
    {
        /// <summary>아직 찾아보지 않았거나 카메라가 준비되지 않았다.</summary>
        NotAttempted,

        /// <summary>화면에서 물건을 찾았다.</summary>
        Detected,

        /// <summary>물건이 카메라 시야(화면, 최소·최대 거리) 밖에 있다.</summary>
        OutOfView,

        /// <summary>물건이 시야 안에 있지만 다른 물체에 가려 보이지 않는다.</summary>
        Occluded
    }
}
