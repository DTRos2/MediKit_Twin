namespace MediKitTwin.Common
{
    /// <summary>
    /// 베이스가 움직이기 전에 팔이 접는 자세(관절 각도)를 알려 주는 쪽. 서 볼 자리에서 팔이 닿을지 미리 가늠할 때,
    /// 지금 자세가 아니라 베이스가 도착한 뒤 팔이 실제로 출발할 이 접힌 자세에서 가늠하기 위해 쓴다.
    /// </summary>
    public interface IArmStowPoseSource
    {
        /// <param name="jointsRad">접는 자세(rad, URDF 기준, 관절 이름 순서). 새 배열이다.</param>
        /// <returns>이동 전에 팔을 접도록 되어 있으면 true. 접지 않고 이동하면 false.</returns>
        bool TryGetStowPose(out float[] jointsRad);
    }
}
