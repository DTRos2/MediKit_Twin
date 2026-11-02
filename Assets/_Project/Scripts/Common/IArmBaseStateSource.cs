using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 팔 베이스가 지금 월드 어디에 어느 방향으로 있는지, 베이스 루트가 어느 방향을 보는지 알려 주는 쪽.
    /// 서 보기 전에 서는 자리에서 팔이 닿을지 가늠하는 데 쓴다. 알려 줄 수 없는 상태(준비 전 등)면 false를 돌려준다.
    /// </summary>
    public interface IArmBaseStateSource
    {
        /// <param name="armBaseWorldPose">팔 베이스 링크의 월드 자세.</param>
        /// <param name="rootYawDeg">베이스 루트의 수평 방향(도, 월드 Y축 기준).</param>
        bool TryGetState(out Pose armBaseWorldPose, out float rootYawDeg);
    }
}
