using MediKitTwin.ArmA;
using UnityEditor;
using UnityEngine;

// Play 중 순기구학(KinematicChain)이 실제 시뮬레이션 TCP와 맞는지 검사한다.
// 관절을 움직인 자세에서 오차가 크면 관절 축 방향(부호) 가정이 틀린 것이다. IK가 엉뚱한 곳으로 가기 전에 여기서 잡는다.
public static class ArmKinematicsValidator
{
    [MenuItem("Tools/Arm/Validate kinematics (selected robot, Play mode)")]
    private static void Run()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("Play 모드에서 실행한다.");
            return;
        }

        GameObject selected = Selection.activeGameObject;
        ArmController arm = selected != null ? selected.GetComponentInParent<ArmController>() : null;
        if (arm == null || arm.Chain == null)
        {
            Debug.LogError("Hierarchy에서 ArmController가 있는 로봇(또는 그 하위)을 선택한다.");
            return;
        }

        var angles = new float[arm.JointCount];
        arm.GetJointPositions(angles);

        Pose predicted = arm.Chain.Forward(angles);
        Pose actual = new Pose(
            arm.BaseLink.InverseTransformPoint(arm.ToolPose.position),
            Quaternion.Inverse(arm.BaseLink.rotation) * arm.ToolPose.rotation);

        float positionError = Vector3.Distance(predicted.position, actual.position);
        float rotationError = Quaternion.Angle(predicted.rotation, actual.rotation);
        bool ok = positionError < 0.005f && rotationError < 2f;

        string message = $"[{arm.name}] 순기구학 오차: 위치 {positionError * 1000f:F2}mm, 회전 {rotationError:F2}도, 관절(도) = "
            + string.Join(", ", System.Array.ConvertAll(angles, a => (a * Mathf.Rad2Deg).ToString("F1")));

        if (ok)
        {
            Debug.Log(message + "  → 정상");
        }
        else
        {
            Debug.LogError(message + "  → 오차가 크다. 관절을 움직인 자세인지 확인하고, 0도 자세에서도 크면 TCP/베이스 링크 설정을 점검한다.");
        }
    }
}
