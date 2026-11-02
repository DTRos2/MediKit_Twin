using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 굴러가는 베이스를 이동 명령 없이 곧바로 원하는 자리와 방향에 놓는 쪽. 키보드로 위치를 맞춰 보는 도구가 쓴다.
    /// 자동 이동(IMobileBase.DriveTo)과 달리 목표로 굴러가지 않고, 진행 중이던 이동 명령은 취소된다.
    /// </summary>
    public interface IManualPlacement
    {
        /// <param name="worldPosition">놓을 월드 위치. 높이(y)는 무시하고 지금 높이를 유지한다.</param>
        /// <param name="yawDeg">월드 Y축 기준 방향(도).</param>
        void PlaceAt(Vector3 worldPosition, float yawDeg);
    }
}
