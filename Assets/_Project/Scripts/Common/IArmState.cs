using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 로봇암의 현재 상태를 읽는 쪽. 강화학습 관측, IK 시드, 대시보드가 이 인터페이스만 보고 구체 클래스는 모른다.
    /// </summary>
    public interface IArmState
    {
        /// <summary>URDF 관절 이름. 배열 인자의 순서는 항상 이 순서다.</summary>
        IReadOnlyList<string> JointNames { get; }

        /// <summary>도구 끝점(TCP)의 월드 자세. 접근 방향이 +Z(forward)다.</summary>
        Pose ToolPose { get; }

        /// <summary>현재 관절 각도(rad)를 JointNames 순서대로 채운다. 배열 길이는 JointNames.Count와 같아야 한다.</summary>
        void GetJointPositions(float[] anglesRad);

        /// <summary>현재 관절 속도(rad/s)를 JointNames 순서대로 채운다.</summary>
        void GetJointVelocities(float[] velocitiesRadPerSec);

        /// <summary>관절 한계(rad)를 JointNames 순서대로 채운다.</summary>
        void GetJointLimits(float[] lowerRad, float[] upperRad);
    }
}
