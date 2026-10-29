using System.IO;
using MediKitTwin.ArmA;
using UnityEditor;
using UnityEngine;

// 로봇별 ArmProfileSO를 기본값으로 만든다. 링크 이름은 각 URDF의 link name이다.
// TCP 위치는 URDF 크기에서 추정한 값이라 Scene 뷰의 TCP 기즈모(파랑 +Z = 접근 방향)로 확인하고 Inspector에서 다듬는다.
public static class ArmProfileFactory
{
    private const string Folder = "Assets/_Project/Config";

    [MenuItem("Tools/Arm/Create FR5 Profile")]
    private static void CreateFr5()
    {
        var profile = ScriptableObject.CreateInstance<ArmProfileSO>();
        profile.Configure(
            "base_link",
            new[]
            {
                new ArmProfileSO.JointEntry("j1", "shoulder_Link"),
                new ArmProfileSO.JointEntry("j2", "upperarm_Link"),
                new ArmProfileSO.JointEntry("j3", "forearm_Link"),
                new ArmProfileSO.JointEntry("j4", "wrist1_Link"),
                new ArmProfileSO.JointEntry("j5", "wrist2_Link"),
                new ArmProfileSO.JointEntry("j6", "wrist3_Link"),
            },
            90f,
            "hand_base_link",
            new Vector3(0f, 0.11f, 0f),
            new Vector3(-90f, 0f, 0f),
            new[]
            {
                // 열림/닫힘 방향은 URDF만으로 알 수 없다. 열고 닫아 보고 반대면 두 값을 바꾼다.
                new ArmProfileSO.GripperJointEntry("finger_link1", 0.04f, 0f),
                new ArmProfileSO.GripperJointEntry("finger_link2", 0.04f, 0f),
            });

        Save(profile, "ArmProfile_FR5");
    }

    [MenuItem("Tools/Arm/Create UR3 Robotiq Profile")]
    private static void CreateUr3()
    {
        var profile = ScriptableObject.CreateInstance<ArmProfileSO>();
        profile.Configure(
            "base_link",
            new[]
            {
                new ArmProfileSO.JointEntry("shoulder_pan_joint", "shoulder_link"),
                new ArmProfileSO.JointEntry("shoulder_lift_joint", "upper_arm_link"),
                new ArmProfileSO.JointEntry("elbow_joint", "forearm_link"),
                new ArmProfileSO.JointEntry("wrist_1_joint", "wrist_1_link"),
                new ArmProfileSO.JointEntry("wrist_2_joint", "wrist_2_link"),
                new ArmProfileSO.JointEntry("wrist_3_joint", "wrist_3_link"),
            },
            90f,
            "robotiq_arg2f_base_link",
            new Vector3(0f, 0.17f, 0f),
            new Vector3(-90f, 0f, 0f),
            new[]
            {
                // Robotiq 2F-140은 finger_joint(0~0.7)를 기준으로 나머지 5개가 mimic(multiplier -1 또는 1)으로 따라간다.
                new ArmProfileSO.GripperJointEntry("left_outer_knuckle", 0f, 0.7f),
                new ArmProfileSO.GripperJointEntry("left_inner_knuckle", 0f, -0.7f),
                new ArmProfileSO.GripperJointEntry("left_inner_finger", 0f, 0.7f),
                new ArmProfileSO.GripperJointEntry("right_outer_knuckle", 0f, -0.7f),
                new ArmProfileSO.GripperJointEntry("right_inner_knuckle", 0f, -0.7f),
                new ArmProfileSO.GripperJointEntry("right_inner_finger", 0f, 0.7f),
            });

        Save(profile, "ArmProfile_UR3_Robotiq");
    }

    private static void Save(ArmProfileSO profile, string fileName)
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Config");
        }

        string path = $"{Folder}/{fileName}.asset";
        if (File.Exists(path))
        {
            Debug.LogWarning($"{path}가 이미 있다. 덮어쓰지 않았다. 지우고 다시 만들거나 Inspector에서 수정한다.");
            Object.DestroyImmediate(profile);
            return;
        }

        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(profile);
        Debug.Log($"{path}를 만들었다.");
    }
}
