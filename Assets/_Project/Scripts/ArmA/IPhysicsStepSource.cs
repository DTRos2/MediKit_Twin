using System;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물리 스텝마다 알려 주는 쪽. 쥔 물건은 물리 스텝마다 움직이므로, 그 사이의 변화를 보려면 프레임이 아니라 스텝 단위로 재야 한다.
    /// 시험에서는 가짜가 원하는 만큼 스텝을 흘려 보낸다.
    /// </summary>
    public interface IPhysicsStepSource
    {
        /// <summary>물리 스텝 하나마다 알린다.</summary>
        event Action Stepped;
    }
}
