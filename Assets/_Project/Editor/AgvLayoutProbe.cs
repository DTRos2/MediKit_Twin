using System.Text;
using ConveyorSystem;
using UnityEditor;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 하역 AGV(UnloaderRobot) 작업 전에 배치를 재서 Console에 숫자로 남긴다. 아무것도 옮기거나 바꾸지 않는다.
    /// Conv_Packing_04 끝점과 벨트 윗면 높이, UnloaderRobot의 위치·방향·박스 부착점(Point_box), AGVPath 웨이포인트,
    /// UnloadingDock 위치를 월드 좌표로 읽는다. 받침 플랫폼 위치와 로봇/경로 이동량을 숫자로 정하기 위한 도구이다.
    /// </summary>
    public static class AgvLayoutProbe
    {
        [MenuItem("Tools/MediKit/Measure AGV Layout")]
        public static void Measure()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[AgvLayoutProbe] 월드 좌표 기준 (Cell 이 항등 변환이 아니면 로컬과 다르다)");

            AppendConveyor(sb);
            AppendRobot(sb);
            AppendPath(sb);
            AppendDock(sb);

            Debug.Log(sb.ToString());
        }

        private static void AppendConveyor(StringBuilder sb)
        {
            Transform conv = FindByName("Conv_Packing_04");
            if (conv == null)
            {
                sb.AppendLine("Conv_Packing_04: 씬에 없음");
                return;
            }

            ConveyorBase conveyor = conv.GetComponentInChildren<ConveyorBase>();
            Bounds bounds = BoundsOf(conv.gameObject);
            sb.AppendLine($"Conv_Packing_04: 위치 {Fmt(conv.position)}, 앞방향(forward) {Fmt(conv.forward)}, 오른쪽(right) {Fmt(conv.right)}");
            sb.AppendLine($"  모델 범위 min {Fmt(bounds.min)} max {Fmt(bounds.max)} (크기 {Fmt(bounds.size)})");
            if (conveyor != null)
            {
                sb.AppendLine($"  CenterPoint {Fmt(conveyor.CenterPoint.position)} (벨트 윗면 높이 y={conveyor.CenterPoint.position.y:F3})");
            }
            else
            {
                sb.AppendLine("  ConveyorBase 없음: 벨트 윗면 높이는 모델 범위 max.y 로 본다");
            }
        }

        private static void AppendRobot(StringBuilder sb)
        {
            Transform robot = FindByName("UnloaderRobot");
            if (robot == null)
            {
                sb.AppendLine("UnloaderRobot: 씬에 없음");
                return;
            }

            Bounds bounds = BoundsOf(robot.gameObject);
            sb.AppendLine($"UnloaderRobot: 위치 {Fmt(robot.position)}, 앞방향(forward) {Fmt(robot.forward)}, yaw {robot.eulerAngles.y:F1}도");
            sb.AppendLine($"  모델 범위 min {Fmt(bounds.min)} max {Fmt(bounds.max)} (크기 {Fmt(bounds.size)})");

            Transform point = FindChildByName(robot, "Point_box");
            if (point != null)
            {
                sb.AppendLine($"  Point_box: 월드 {Fmt(point.position)}, 로봇 기준(로컬) {Fmt(robot.InverseTransformPoint(point.position))}");
            }
            else
            {
                sb.AppendLine("  Point_box 없음");
            }

            var interactor = robot.GetComponentInChildren<Warcool.WarehouseRobot.RobotCargoInteractor>(true);
            sb.AppendLine(interactor != null
                ? $"  RobotCargoInteractor: createStableCarryRoot={interactor.createStableCarryRoot}, carriedBoxLocalPosition={Fmt(interactor.carriedBoxLocalPosition)}"
                : "  RobotCargoInteractor 없음");
        }

        private static void AppendPath(StringBuilder sb)
        {
            Transform path = FindByName("AGVPath");
            if (path == null)
            {
                sb.AppendLine("AGVPath: 씬에 없음");
                return;
            }

            sb.AppendLine($"AGVPath: 위치 {Fmt(path.position)}, 자식 {path.childCount}개");
            for (int i = 0; i < path.childCount; i++)
            {
                Transform child = path.GetChild(i);
                sb.AppendLine($"  [{i}] {child.name}: {Fmt(child.position)}");
            }
        }

        private static void AppendDock(StringBuilder sb)
        {
            Transform dock = FindByName("UnloadingDock");
            if (dock == null)
            {
                sb.AppendLine("UnloadingDock: 씬에 없음");
                return;
            }

            Bounds bounds = BoundsOf(dock.gameObject);
            sb.AppendLine($"UnloadingDock: 위치 {Fmt(dock.position)}, 모델 범위 min {Fmt(bounds.min)} max {Fmt(bounds.max)}");
        }

        private static Transform FindByName(string name)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }

        private static Transform FindChildByName(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }

        private static Bounds BoundsOf(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(go.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static string Fmt(Vector3 v) => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
    }
}
