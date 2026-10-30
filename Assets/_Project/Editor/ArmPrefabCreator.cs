using System.IO;
using MediKitTwin.ArmA;
using MediKitTwin.Demo;
using UnityEditor;
using UnityEngine;

// 씬의 fr5_robot, ur3_with_gripper를 프리팹으로 저장하고 씬 오브젝트를 그 프리팹의 인스턴스로 연결한다.
// 컨벤션: 프리팹 접두사 PF_, 한글과 공백 없음, 기능은 프리팹으로 만들고 씬에는 인스턴스만 둔다.
public static class ArmPrefabCreator
{
    private const string Folder = "Assets/_Project/Prefabs";

    [MenuItem("Tools/Arm/Create robot prefabs (FR5, UR3)")]
    private static void CreateBoth()
    {
        CreatePrefab("fr5_robot", "PF_FR5Robot");
        CreatePrefab("ur3_with_gripper", "PF_UR3Robot");
    }

    private static void CreatePrefab(string sceneObjectName, string prefabName)
    {
        GameObject robot = GameObject.Find(sceneObjectName);
        if (robot == null)
        {
            Debug.LogWarning($"씬에서 '{sceneObjectName}'를 찾지 못해 {prefabName}을(를) 건너뛴다.");
            return;
        }

        if (PrefabUtility.IsPartOfPrefabInstance(robot))
        {
            Debug.LogWarning($"'{sceneObjectName}'는 이미 프리팹 인스턴스다. {prefabName}을(를) 건너뛴다.");
            return;
        }

        if (robot.GetComponent<ArmController>() == null)
        {
            Debug.LogWarning($"'{sceneObjectName}'에 ArmController가 없다. Tools > Arm > Setup selected robot 을 먼저 실행한다.");
            return;
        }

        string path = $"{Folder}/{prefabName}.prefab";
        if (File.Exists(path))
        {
            Debug.LogWarning($"{path}가 이미 있어서 덮어쓰지 않았다. 지우고 다시 만들거나 Prefab Mode에서 수정한다.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
        }

        // 프리팹은 씬 오브젝트를 참조할 수 없다. 시험 패널이 가리키는 큐브 참조를 잠시 떼었다가 저장한 뒤 인스턴스에 다시 건다.
        ArmDemoPanel panel = robot.GetComponent<ArmDemoPanel>();
        Object pickTarget = ReadPickTarget(panel);
        WritePickTarget(panel, null);

        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(robot, path, InteractionMode.UserAction);

        WritePickTarget(panel, pickTarget);
        if (panel != null)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(panel);
        }

        Debug.Log($"{path}를 만들고 씬의 '{sceneObjectName}'를 인스턴스로 연결했다.", prefab);
    }

    private static Object ReadPickTarget(ArmDemoPanel panel)
    {
        if (panel == null)
        {
            return null;
        }

        return new SerializedObject(panel).FindProperty("_pickTarget").objectReferenceValue;
    }

    private static void WritePickTarget(ArmDemoPanel panel, Object value)
    {
        if (panel == null)
        {
            return;
        }

        var serialized = new SerializedObject(panel);
        serialized.FindProperty("_pickTarget").objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
    }
}
