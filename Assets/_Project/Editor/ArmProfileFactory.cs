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
        GetOrCreateFr5();
    }

    /// <summary>프로필이 있으면 불러오고 없으면 기본값으로 만든다.</summary>
    public static ArmProfileSO GetOrCreateFr5()
    {
        const string fileName = "ArmProfile_FR5";
        string path = $"{Folder}/{fileName}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<ArmProfileSO>(path);
        if (existing != null)
        {
            return existing;
        }

        Save(BuildFr5(), fileName);
        return AssetDatabase.LoadAssetAtPath<ArmProfileSO>(path);
    }

    private static ArmProfileSO BuildFr5()
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
                // 열림/닫힘 방향은 URDF만으로 알 수 없어 실제로 열고 닫아 확인했다(0이 열림, 0.04가 닫힘). 반대로 움직이면 두 값을 바꾼다.
                new ArmProfileSO.GripperJointEntry("finger_link1", 0f, 0.04f),
                new ArmProfileSO.GripperJointEntry("finger_link2", 0f, 0.04f),
            },
            // 윗팔 수직(j2 -90), 앞팔 수평(j3 +90), 그리퍼가 아래를 향하는(j4 -90, j5 -90) 세운 자세.
            // URDF 기구학으로 계산한 값이다. 모든 관절 0도는 팔을 수평으로 뻗어 로봇이 누운 것처럼 보인다.
            new[] { 0f, -90f, 90f, -90f, -90f, 0f });

        return profile;
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
