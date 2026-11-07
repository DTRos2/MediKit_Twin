using System.Collections.Generic;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 로봇암 A의 관절 목표를 주는 쪽. ArmController는 목표를 누가 줬는지 모른다.
    /// 구현체: IkArmTarget(Unity 단독, 필수), RosArmTarget(/arm_a/joint_command 수신, 선택)
    /// </summary>
    public interface IArmTarget
    {
        /// <summary>URDF 관절 이름. 순서는 구현체가 바꾸지 않는다.</summary>
        IReadOnlyList<string> JointNames { get; }

        /// <summary>
        /// 관절 목표 각도(rad)를 JointNames 순서대로 anglesRad에 채운다.
        /// 목표가 없으면 false를 반환하고 배열은 건드리지 않는다.
        /// anglesRad의 길이는 JointNames.Count와 같아야 하며, 호출하는 쪽이 한 번 만들어 재사용한다.
        /// </summary>
        bool TryGetTargets(float[] anglesRad);
    }
}
