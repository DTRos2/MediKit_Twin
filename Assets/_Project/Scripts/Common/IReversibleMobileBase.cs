using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 뒤로 물러날 수 있는 굴러가는 베이스. IMobileBase의 DriveTo는 목표 쪽으로 제자리에서 돌고 나서 전진하므로,
    /// 앞에 물체가 바짝 있어 돌 수 없는 자리(받침대 앞)에서는 쓸 수 없다. 이런 자리에서는 먼저 뒤로 물러난 뒤 돈다. (FR-12)
    /// </summary>
    public interface IReversibleMobileBase : IMobileBase
    {
        /// <summary>
        /// 방향을 바꾸지 않고 뒤로 물러나 목표 위치(바닥 평면, 높이는 무시)에 선다. 도착하면 Arrived가 발생한다.
        /// 목표가 로봇의 뒤쪽이 아니면 등이 목표를 향하도록 먼저 제자리에서 돈다.
        /// </summary>
        void ReverseTo(Vector3 worldPosition);
    }
}
