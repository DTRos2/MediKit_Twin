using System.Text;
using Unity.Robotics.UrdfImporter;
using UnityEditor;
using UnityEngine;

// 선택한 로봇 루트(없으면 이름이 fr5_robot인 오브젝트) 아래 모든 UrdfLink의 변환 값과 visual 렌더러 경계를 로그로 찍는다.
public static class UrdfLinkTransformDump
{
    [MenuItem("Tools/URDF/Dump link transforms (selected robot)")]
    static void Run()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null) root = GameObject.Find("fr5_robot");
        if (root == null)
        {
            Debug.LogError("Hierarchy에서 로봇 루트(fr5_robot)를 선택하세요.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[링크 변환 덤프] root={root.name}");
        foreach (var link in root.GetComponentsInChildren<UrdfLink>(true))
        {
            Transform t = link.transform;
            sb.AppendLine($"{link.name,-14} world=({t.position.x:F3},{t.position.y:F3},{t.position.z:F3}) worldEuler={t.eulerAngles:F1}");

            // 이 링크 자신의 Visuals 아래 렌더러만 (자식 링크의 것은 제외)
            Transform visuals = t.Find("Visuals");
            if (visuals == null) continue;
            foreach (var r in visuals.GetComponentsInChildren<Renderer>(true))
            {
                Bounds b = r.bounds;
                sb.AppendLine($"    visual '{r.name}' (부모 {r.transform.parent.name}) localPos={r.transform.localPosition:F3} " +
                              $"localEuler={r.transform.localEulerAngles:F1} localScale={r.transform.localScale:F3} " +
                              $"boundsCenter=({b.center.x:F3},{b.center.y:F3},{b.center.z:F3}) size=({b.size.x:F3},{b.size.y:F3},{b.size.z:F3})");
            }
        }

        Debug.Log(sb.ToString());
    }
}
