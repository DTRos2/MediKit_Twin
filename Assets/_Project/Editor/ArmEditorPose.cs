using MediKitTwin.ArmA;
using UnityEditor;
using UnityEngine;

// Play 없이도 로봇이 세워진 자세로 보이도록 편집 모드에서 링크를 시작 자세만큼 돌려 둔다.
// 돌린 각도는 ArmController에 기록해 두어서, Play 때 ArmController가 Unity의 관절 기준 해석(0도 기준이 밀렸는지)을
// 판별해 URDF 각도를 일정하게 유지한다. 로봇이 프리팹 인스턴스면 프리팹에도 바로 반영한다.
public static class ArmEditorPose
{
    private const string EditorPoseField = "_editorPoseRad";

    [MenuItem("Tools/Arm/Apply home pose to robots (edit mode)")]
    private static void ApplyToAll()
    {
        Process(apply: true);
    }

    [MenuItem("Tools/Arm/Revert robots to zero pose (edit mode)")]
    private static void RevertAll()
    {
        Process(apply: false);
    }

    private static void Process(bool apply)
    {
        if (Application.isPlaying)
        {
            Debug.LogError("Play를 끄고 실행한다.");
            return;
        }

        int done = 0;
        foreach (ArmController arm in Object.FindObjectsByType<ArmController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (arm.gameObject.scene.IsValid() && ProcessRobot(arm, apply))
            {
                done++;
            }
        }

        if (done == 0)
        {
            Debug.LogWarning("처리한 로봇이 없다. Tools > Arm > Setup selected robot 으로 ArmController를 먼저 붙인다.");
        }
    }

    private static bool ProcessRobot(ArmController arm, bool apply)
    {
        ArmProfileSO profile = arm.Profile;
        if (profile == null)
        {
            Debug.LogError($"{arm.name}: ArmController에 프로필이 없다.", arm);
            return false;
        }

        if (!ArmRig.TryResolve(arm.transform, profile, out ArmRig rig, out string error))
        {
            Debug.LogError($"{arm.name}: {error}", arm);
            return false;
        }

        // 이미 돌려 둔 자세가 있으면 먼저 원래대로 되돌려 URDF 0도 자세에서 시작한다.
        float[] previous = ReadEditorPose(arm, rig.Joints.Length);
        if (previous != null)
        {
            Rotate(rig, previous, -1f);
        }

        if (!apply)
        {
            WriteEditorPose(arm, new float[0]);
            Finish(arm, $"{arm.name}: 모든 관절 0도 자세로 되돌렸다.");
            return true;
        }

        if (!profile.HasHomePose)
        {
            WriteEditorPose(arm, new float[0]);
            Debug.LogWarning($"{arm.name}: 프로필에 시작 자세(Home Pose Deg)가 없어 돌리지 않았다.", arm);
            return false;
        }

        // 링크가 URDF 0도 자세인 지금 순기구학을 만들어, 부호가 맞는 시작 자세를 고른다.
        var zeros = new float[rig.Joints.Length];
        KinematicChain chain = KinematicChainBuilder.Build(
            rig.Base, rig.Joints, rig.Tool, profile.ToolPositionOffset, profile.ToolRotationOffset, zeros);
        float[] home = HomePoseSelector.Choose(profile, chain, rig.Base);

        Rotate(rig, home, 1f);
        WriteEditorPose(arm, home);
        Finish(arm, $"{arm.name}: 시작 자세로 링크를 돌려 두었다 (도: {string.Join(", ", System.Array.ConvertAll(home, a => (a * Mathf.Rad2Deg).ToString("F0")))}).");
        return true;
    }

    // 각 링크의 로컬 회전에 관절 회전을 곱한다. 링크는 서로 중첩된 Transform이라 아래쪽 링크가 함께 따라간다.
    private static void Rotate(ArmRig rig, float[] anglesRad, float direction)
    {
        for (int i = 0; i < rig.Joints.Length; i++)
        {
            ArticulationBody body = rig.Joints[i];
            if (body.anchorPosition.sqrMagnitude > 1e-8f)
            {
                Debug.LogWarning($"{body.name}: 관절 피벗이 링크 원점이 아니라 위치가 조금 어긋날 수 있다.", body);
            }

            Vector3 axis = (body.anchorRotation * Vector3.right).normalized;
            Transform link = body.transform;
            Undo.RecordObject(link, "Arm home pose");
            link.localRotation = link.localRotation * Quaternion.AngleAxis(direction * anglesRad[i] * Mathf.Rad2Deg, axis);
        }
    }

    private static float[] ReadEditorPose(ArmController arm, int jointCount)
    {
        SerializedProperty property = new SerializedObject(arm).FindProperty(EditorPoseField);
        if (property == null || property.arraySize != jointCount)
        {
            return null;
        }

        var values = new float[jointCount];
        for (int i = 0; i < jointCount; i++)
        {
            values[i] = property.GetArrayElementAtIndex(i).floatValue;
        }

        return values;
    }

    private static void WriteEditorPose(ArmController arm, float[] values)
    {
        var serialized = new SerializedObject(arm);
        SerializedProperty property = serialized.FindProperty(EditorPoseField);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).floatValue = values[i];
        }

        serialized.ApplyModifiedProperties();
    }

    private static void Finish(ArmController arm, string message)
    {
        EditorUtility.SetDirty(arm);
        GameObject root = arm.gameObject;
        if (PrefabUtility.IsPartOfPrefabInstance(root))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(arm);
            GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(root);
            PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);
            message += " 프리팹에도 반영했다.";
        }

        Debug.Log(message, arm);
    }
}
