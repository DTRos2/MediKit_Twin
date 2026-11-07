using System;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔 베이스를 옮기는 동안 단계(접었다, 도착했다)를 알려 주는 쪽. 진단이 쥔 물건이 언제 손에서 밀리는지 가르는 데 쓴다.
    /// IArmBasePositioner에 넣지 않은 것은 모든 이동 방식이 단계를 알려야 하는 것은 아니기 때문이다.
    /// </summary>
    public interface IArmPhaseSource
    {
        /// <summary>단계에 닿을 때마다 알린다. 같은 단계가 한 주문 안에서 여러 번 나올 수 있다.</summary>
        event Action<ArmMotionPhase> PhaseReached;
    }
}
