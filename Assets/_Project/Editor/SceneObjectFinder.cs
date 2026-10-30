using System.Collections.Generic;
using System.Linq;
using MediKitTwin.ArmA;
using UnityEditor;
using UnityEngine;

// 에디터 도구들이 열린 씬에서 오브젝트를, 프로젝트에서 에셋을 찾는 방법을 한 곳에 모은다.
// 도구마다 같은 규칙으로 찾아야 "어느 로봇인지"가 도구마다 달라지는 일이 없다.
public static class SceneObjectFinder
{
    public const string DefaultRobotName = "fr5_robot";

    /// <summary>씬에서 해당 컴포넌트가 정확히 하나일 때만 쓴다. 없거나 여럿이면 어느 것인지 알 수 없으므로 멈추고 알린다.</summary>
    public static bool TryFindSingle<T>(out T found, string label) where T : Object
    {
        T[] all = Object.FindObjectsByType<T>(FindObjectsInactive.Include);
        found = all.Length == 1 ? all[0] : null;
        if (all.Length != 1)
        {
            Debug.LogError($"[SceneObjectFinder] 열린 씬에 {label}가 {all.Length}개 있다. 정확히 1개여야 한다.");
        }

        return found != null;
    }

    /// <summary>로봇암은 선택한 오브젝트(또는 그 부모)의 ArmController를 먼저 쓰고, 선택이 없으면 fr5_robot이 하나일 때 쓴다.</summary>
    public static bool TryFindRobot(out ArmController arm)
    {
        arm = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<ArmController>() : null;
        if (arm != null)
        {
            return true;
        }

        ArmController[] candidates = Object.FindObjectsByType<ArmController>(FindObjectsInactive.Include)
            .Where(controller => controller.name == DefaultRobotName)
            .ToArray();
        if (candidates.Length == 1)
        {
            arm = candidates[0];
            return true;
        }

        Debug.LogError($"[SceneObjectFinder] '{DefaultRobotName}' 로봇이 {candidates.Length}개 있다. Hierarchy에서 키팅할 로봇을 선택한 뒤 다시 실행한다.");
        return false;
    }

    public static T FindAsset<T>() where T : Object
    {
        List<T> assets = FindAssets<T>();
        return assets.Count > 0 ? assets[0] : null;
    }

    public static List<T> FindAssets<T>() where T : Object
    {
        return AssetDatabase.FindAssets($"t:{typeof(T).Name}")
            .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(asset => asset != null)
            .ToList();
    }
}
