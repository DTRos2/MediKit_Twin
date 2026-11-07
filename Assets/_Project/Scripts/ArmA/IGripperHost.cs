using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 그리퍼가 붙은 로봇이 그리퍼에게 알려 주는 것. JointGripper는 이 인터페이스만 알아서
    /// 팔 구동(ArmController)과 상관없이 그리퍼 관절 목록과 도구 위치만 있으면 어디에든 붙는다.
    /// </summary>
    public interface IGripperHost
    {
        /// <summary>그리퍼 관절 링크를 찾기 시작할 최상위 Transform.</summary>
        Transform RobotRoot { get; }

        IReadOnlyList<ArmProfileSO.GripperJointEntry> GripperJoints { get; }

        /// <summary>쥔 물체를 고정 조인트로 붙일 때 기준이 되는 ArticulationBody. 도구 링크의 것.</summary>
        ArticulationBody ToolBody { get; }

        /// <summary>도구 끝점(두 손가락 사이)의 월드 자세. 물체를 찾는 중심이다.</summary>
        Pose ToolPose { get; }
    }
}
