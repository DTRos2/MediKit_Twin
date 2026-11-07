namespace MediKitTwin.Common
{
    /// <summary>
    /// 컨베이어에 실려 있는 키트 박스를 컨베이어에서 풀어 물리로 움직이게 하는 접점. (FR-05, FR-09)
    /// 푸셔는 컨베이어 패키지를 모르고, 박스가 이 접점을 가지고 있으면 밀기 직전에 부른다. 없으면 이미 물리로 움직이는 박스로 본다.
    /// </summary>
    public interface IBoxConveyorRelease
    {
        /// <summary>컨베이어가 박스를 더 이상 붙잡지 않게 하고, 박스를 물리로 움직일 수 있는 상태로 만든다.</summary>
        void ReleaseToPhysics();
    }
}
