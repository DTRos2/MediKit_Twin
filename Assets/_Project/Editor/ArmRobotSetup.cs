using System;
using System.Linq;
using MediKitTwin.ArmA;
using UnityEditor;
using UnityEngine;

// 선택한 로봇 하나에 팔 구동, IK, 그리퍼, 집기 작업, 시험 패널을 한 번에 붙이고 서로 연결한다.
// 수동으로 하면 필드 연결을 하나만 빠뜨려도 조용히 동작하지 않아서 도구로 만들었다. 이미 붙어 있으면 새로 만들지 않고 연결만 다시 한다.
// 씬은 저장하지 않는다. 결과를 확인하고 Ctrl+S로 직접 저장한다.
public static class ArmRobotSetup
{
    [MenuItem("Tools/Arm/Setup selected robot as FR5")]
    private static void SetupFr5()
    {
        Setup(ArmProfileFactory.GetOrCreateFr5(), "fr5_robot", new Vector2(10f, 10f), "FR5 Demo", new Vector3(0f, 0.03f, -0.4f));
    }

    private static void Setup(ArmProfileSO profile, string defaultRobotName, Vector2 panelPosition, string title, Vector3 cubeOffset)
    {
        // 선택이 없으면 기본 이름으로 씬에서 찾는다. 씬 오브젝트가 아닌 에셋을 선택한 경우도 선택 없음으로 본다.
        GameObject robot = Selection.activeGameObject != null && Selection.activeGameObject.scene.IsValid()
            ? Selection.activeGameObject
            : GameObject.Find(defaultRobotName);

        if (robot == null || profile == null)
        {
            Debug.LogError($"로봇을 찾지 못했다. Hierarchy에서 로봇 최상위 오브젝트('{defaultRobotName}')를 클릭해 선택한 뒤 다시 실행한다.");
            return;
        }

        // 하위 링크를 골랐으면 최상위 로봇으로 올라간다.
        Transform root = robot.transform;
        while (root.parent != null && root.GetComponentInChildren<ArticulationBody>(true) == null)
        {
            root = root.parent;
        }

        robot = root.gameObject;
        if (!HasLink(robot, profile.BaseLinkName) || !HasLink(robot, profile.ToolLinkName))
        {
            Debug.LogError($"{robot.name}에서 링크 '{profile.BaseLinkName}' 또는 '{profile.ToolLinkName}'를 찾지 못했다. 선택한 로봇과 프로필이 맞는지 확인한다.");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName($"Setup {robot.name}");

        RemoveImporterController(robot);

        var arm = GetOrAdd<ArmController>(robot);
        var ik = GetOrAdd<IkArmTarget>(robot);
        var gripper = GetOrAdd<JointGripper>(robot);
        var task = GetOrAdd<PickAndLiftTask>(robot);
        var panel = GetOrAdd<ArmDemoPanel>(robot);
        Graspable cube = GetOrCreateCube(robot, cubeOffset);

        Set(arm, "_profile", profile);
        Set(arm, "_targetSource", ik);
        Set(ik, "_arm", arm);
        Set(gripper, "_arm", arm);
        Set(task, "_toolTarget", ik);
        Set(task, "_gripper", gripper);
        Set(panel, "_title", title);
        Set(panel, "_arm", arm);
        Set(panel, "_ikTarget", ik);
        Set(panel, "_gripper", gripper);
        Set(panel, "_task", task);
        Set(panel, "_pickTarget", cube);
        SetVector2(panel, "_screenPosition", panelPosition);

        EditorUtility.SetDirty(robot);
        Debug.Log($"[{robot.name}] 설정 완료: ArmController, IkArmTarget, JointGripper, PickAndLiftTask, ArmDemoPanel 연결, 시험 큐브 '{cube.name}'. " +
                  "씬을 저장하려면 Ctrl+S. 큐브는 추정 위치이므로 IK가 실패하면 로봇이 닿는 곳으로 옮긴다.");
    }

    private static bool HasLink(GameObject robot, string linkName)
    {
        return robot.GetComponentsInChildren<Transform>(true).Any(t => t.name == linkName);
    }

    // URDF Importer의 Controller는 UnityEngine.Input을 읽어서 Input System 전용 프로젝트에서 매 프레임 예외가 난다.
    // 임포터 어셈블리에 의존하지 않도록 형식 이름으로 찾는다.
    private static void RemoveImporterController(GameObject robot)
    {
        foreach (Component component in robot.GetComponents<Component>())
        {
            if (component == null)
            {
                continue;
            }

            string typeName = component.GetType().Name;
            if (typeName == "Controller" || typeName == "FKRobot")
            {
                Undo.DestroyObjectImmediate(component);
            }
        }
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        var existing = target.GetComponent<T>();
        return existing != null ? existing : Undo.AddComponent<T>(target);
    }

    private static Graspable GetOrCreateCube(GameObject robot, Vector3 offset)
    {
        string cubeName = $"TestCube_{robot.name}";
        GameObject existing = GameObject.Find(cubeName);
        if (existing != null && existing.TryGetComponent(out Graspable found))
        {
            return found;
        }

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(cube, "Create test cube");
        cube.name = cubeName;
        cube.transform.localScale = Vector3.one * 0.05f;
        cube.transform.position = robot.transform.position + offset;

        var body = cube.AddComponent<Rigidbody>();
        body.mass = 0.1f;
        return cube.AddComponent<Graspable>();
    }

    // [SerializeField] private 필드를 이름으로 채운다. 필드 이름이 바뀌면 여기서 바로 오류가 나서 연결 누락을 알 수 있다.
    private static void Set(Component target, string fieldName, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null)
        {
            throw new InvalidOperationException($"{target.GetType().Name}에 필드 '{fieldName}'가 없다.");
        }

        property.objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
    }

    private static void Set(Component target, string fieldName, string value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName)
            ?? throw new InvalidOperationException($"{target.GetType().Name}에 필드 '{fieldName}'가 없다.");
        property.stringValue = value;
        serialized.ApplyModifiedProperties();
    }

    private static void SetVector2(Component target, string fieldName, Vector2 value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName)
            ?? throw new InvalidOperationException($"{target.GetType().Name}에 필드 '{fieldName}'가 없다.");
        property.vector2Value = value;
        serialized.ApplyModifiedProperties();
    }
}
