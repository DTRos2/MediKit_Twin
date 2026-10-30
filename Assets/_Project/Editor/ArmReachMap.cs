using System.Collections.Generic;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using UnityEditor;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 로봇암 B(UR3 + 그리퍼)가 닿는 범위를 재서 뚜껑 박스와 컨베이어 위 키트 박스의 배치를 정하는 도구. 위치는 바꾸지 않고 읽기만 한다.
    /// 플레이 모드에서 Tools > MediKit > Measure ArmB Reach를 실행하면, 팀원의 IK(ArmController, MultiStartInverseKinematics)로
    /// 그리퍼를 아래로 향한 채 두 높이(뚜껑을 잡는 높이, 키트 박스를 덮는 높이)에서 바닥 격자를 훑는다.
    /// 결과는 Scene 뷰에 점으로 그리고(초록=잡기, 파랑=덮기 닿음, 연한 빨강=닿지 않음) 콘솔에 범위와 현재 배치 판정을 적는다.
    /// 관절 한계는 반영하지만 충돌(받침대, 벽, 뚜껑 박스)은 보지 않는다.
    /// </summary>
    public static class ArmReachMap
    {
        // 훑는 범위(로봇 베이스 둘레, m)와 격자 간격(m)
        private const float Range = 0.8f;
        private const float Step = 0.05f;

        // 키트 박스 윗면(컨베이어 윗면 기준 0.135m)에 덮은 뚜껑 윗판이 올 높이 여유(m). 뚜껑 높이 0.04의 절반 정도.
        private const float CoverClearance = 0.02f;

        // 뚜껑을 잡을 때 그리퍼 방향을 돌려 보는 각도(도). 하나라도 풀리면 닿는다고 본다.
        private static readonly float[] Yaws = { 0f, 90f, 180f, 270f };

        private struct Sample
        {
            public Vector3 Position;
            public bool Reachable;
            public Color Color;
        }

        private static readonly List<Sample> Samples = new List<Sample>();

        // 전체 범위 측정(3D) 결과. 점은 닿는 곳만, 링은 작업 높이 두 곳(뚜껑 잡기, 덮기)의 수평 안쪽/바깥쪽 반경이다.
        private struct Ring
        {
            public Vector3 Center;
            public float MinRadius;
            public float MaxRadius;
            public Color Color;
        }

        private static readonly List<Sample> WorkspaceSamples = new List<Sample>();
        private static readonly List<Ring> WorkspaceRings = new List<Ring>();

        [MenuItem("Tools/MediKit/Measure ArmB Reach")]
        public static void Measure()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[ArmReachMap] 플레이 모드에서 실행하세요. (로봇암이 시작할 때 IK용 관절 정보를 만듭니다)");
                return;
            }

            ArmController arm = FindArmB();
            if (arm == null || !arm.IsReady)
            {
                Debug.LogError("[ArmReachMap] UR3 프로필을 쓰는 ArmController가 없거나 아직 준비되지 않았습니다. UR3 로봇에 Tools > Arm > Setup selected robot as UR3를 실행했는지 확인하세요.");
                return;
            }

            DlsIkSettings settings = arm.Profile.Ik;
            IInverseKinematics solver = new MultiStartInverseKinematics(
                new DlsInverseKinematics(arm.Chain, settings), arm.Chain, settings.Restarts, settings.RestartSeed);

            float graspY = LidGraspHeight(out Vector3 lidCenter, out bool hasLid);
            float coverY = CoverHeight(out GameObject sensor);

            Samples.Clear();
            Vector3 origin = arm.BaseLink.position;
            ScanLevel(arm, solver, origin, graspY, new Color(0.1f, 0.85f, 0.2f), out Range2 graspRange, out int graspCount);
            ScanLevel(arm, solver, origin, coverY, new Color(0.2f, 0.45f, 1f), out Range2 coverRange, out int coverCount);

            Debug.Log($"[ArmReachMap] 로봇 베이스 {origin}  (격자 {Step:0.00}m, 반경 {Range:0.0}m, 그리퍼는 아래를 향함)");
            Debug.Log($"[ArmReachMap] 뚜껑 잡는 높이 y={graspY:0.00}: 닿는 칸 {graspCount}개, {graspRange}");
            Debug.Log($"[ArmReachMap] 키트 박스 덮는 높이 y={coverY:0.00}: 닿는 칸 {coverCount}개, {coverRange}");

            if (hasLid) ReportLidEdges(arm, solver, lidCenter);
            else Debug.LogWarning("[ArmReachMap] Lid_01을 찾지 못해 현재 뚜껑 위치 판정은 건너뜁니다.");

            if (sensor == null)
            {
                Debug.LogWarning("[ArmReachMap] Sensor_Packing을 찾지 못해 현재 덮기 위치 판정은 건너뜁니다.");
            }
            else
            {
                List<Transform> kitBoxes = FindKitBoxes(sensor.transform);
                if (kitBoxes.Count == 0)
                    Debug.LogWarning("[ArmReachMap] Sensor_Packing 아래에 이름에 KitBox가 들어간 오브젝트가 없어 덮기 위치 판정은 건너뜁니다.");

                foreach (Transform kitBox in kitBoxes)
                {
                    var coverCenter = new Vector3(kitBox.position.x, coverY, kitBox.position.z);
                    ReportPoint(arm, solver, $"키트 박스 위에 덮기, {kitBox.name} 중심", coverCenter);

                    // 그리퍼는 팔 쪽 뚜껑 가장자리를 잡고 있으므로, 덮을 때 그리퍼 끝은 박스 중심보다 팔 쪽으로 0.235m(뚜껑 반폭) 앞에 온다.
                    Vector3 toArm = arm.BaseLink.position - coverCenter;
                    toArm.y = 0f;
                    if (toArm.sqrMagnitude > 1e-6f)
                        ReportPoint(arm, solver, $"키트 박스 위에 덮기, {kitBox.name}의 팔 쪽 뚜껑 가장자리(그리퍼 끝)", coverCenter + toArm.normalized * 0.235f);
                }
            }

            SceneView.duringSceneGui -= Draw;
            SceneView.duringSceneGui += Draw;
            SceneView.RepaintAll();
        }

        // 그리퍼를 아래로 향했을 때 닿는 높이 범위를 베이스 높이 기준으로 적는다. 받침대 높이와 뚜껑 박스 높이를 정할 때 쓴다.
        // 그리퍼 방향은 하나(0도)만 보고 격자를 성기게(10cm) 훑어 빠르게 끝낸다.
        [MenuItem("Tools/MediKit/Measure ArmB Height Window")]
        public static void MeasureHeightWindow()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[ArmReachMap] 플레이 모드에서 실행하세요.");
                return;
            }

            ArmController arm = FindArmB();
            if (arm == null || !arm.IsReady)
            {
                Debug.LogError("[ArmReachMap] UR3 프로필을 쓰는 ArmController가 없거나 아직 준비되지 않았습니다.");
                return;
            }

            DlsIkSettings settings = arm.Profile.Ik;
            IInverseKinematics solver = new MultiStartInverseKinematics(
                new DlsInverseKinematics(arm.Chain, settings), arm.Chain, settings.Restarts, settings.RestartSeed);

            Vector3 origin = arm.BaseLink.position;
            const float coarse = 0.10f;
            int steps = Mathf.RoundToInt(Range / coarse);

            var log = new System.Text.StringBuilder();
            log.AppendLine($"[ArmReachMap] 그리퍼를 아래로 향했을 때 닿는 높이 (베이스 y={origin.y:0.00})");
            for (float relative = -0.50f; relative <= 0.501f; relative += 0.05f)
            {
                float worldY = origin.y + relative;
                int count = 0;
                float min = float.MaxValue;
                float max = 0f;
                for (int ix = -steps; ix <= steps; ix++)
                {
                    for (int iz = -steps; iz <= steps; iz++)
                    {
                        var position = new Vector3(origin.x + ix * coarse, worldY, origin.z + iz * coarse);
                        if (!TrySolveFirstYaw(arm, solver, position)) continue;

                        count++;
                        float radius = new Vector2(position.x - origin.x, position.z - origin.z).magnitude;
                        min = Mathf.Min(min, radius);
                        max = Mathf.Max(max, radius);
                    }
                }

                string row = count > 0 ? $"수평 {min:0.00} ~ {max:0.00}m" : "닿는 곳 없음";
                log.AppendLine($"  베이스 기준 {relative:+0.00;-0.00;0.00}m (월드 y={worldY:0.00}): {row}");
            }

            Debug.Log(log.ToString());
        }

        // 팔이 닿는 전체 영역을 3D 점으로 그린다. 그리퍼는 아래를 향하고, 높이는 베이스 기준 -0.50 ~ +0.45m를 훑는다.
        // 뚜껑 박스 테이블, 뚜껑 처음 위치, 키트 박스 위치를 팔 둘레의 어느 띠 안에 둘지 정할 때 쓴다.
        [MenuItem("Tools/MediKit/Measure ArmB Workspace (3D)")]
        public static void MeasureWorkspace()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[ArmReachMap] 플레이 모드에서 실행하세요.");
                return;
            }

            ArmController arm = FindArmB();
            if (arm == null || !arm.IsReady)
            {
                Debug.LogError("[ArmReachMap] UR3 프로필을 쓰는 ArmController가 없거나 아직 준비되지 않았습니다.");
                return;
            }

            DlsIkSettings settings = arm.Profile.Ik;
            IInverseKinematics solver = new MultiStartInverseKinematics(
                new DlsInverseKinematics(arm.Chain, settings), arm.Chain, settings.Restarts, settings.RestartSeed);

            const float horizontalStep = 0.10f;
            const float verticalStep = 0.05f;
            const float scanRadius = 0.65f;
            const float lowest = -0.50f;
            const float highest = 0.45f;

            Vector3 origin = arm.BaseLink.position;
            int radiusSteps = Mathf.RoundToInt(scanRadius / horizontalStep);
            int levelCount = Mathf.RoundToInt((highest - lowest) / verticalStep) + 1;

            WorkspaceSamples.Clear();
            WorkspaceRings.Clear();

            var levels = new List<(float worldY, int count, float min, float max)>();
            var log = new System.Text.StringBuilder();
            log.AppendLine($"[ArmReachMap] 닿는 영역 (베이스 {origin}, 그리퍼는 아래를 향함, 격자 {horizontalStep:0.00}m)");

            try
            {
                for (int level = 0; level < levelCount; level++)
                {
                    float relative = lowest + level * verticalStep;
                    float worldY = origin.y + relative;
                    if (EditorUtility.DisplayCancelableProgressBar("ArmB 전체 범위 측정", $"베이스 기준 {relative:+0.00;-0.00;0.00}m", (float)level / levelCount))
                    {
                        Debug.LogWarning("[ArmReachMap] 측정을 취소했습니다. 지금까지의 결과만 그립니다.");
                        break;
                    }

                    int count = 0;
                    float min = float.MaxValue;
                    float max = 0f;
                    Color color = Color.HSVToRGB(Mathf.Lerp(0.66f, 0f, Mathf.InverseLerp(lowest, highest, relative)), 0.85f, 1f);
                    for (int ix = -radiusSteps; ix <= radiusSteps; ix++)
                    {
                        for (int iz = -radiusSteps; iz <= radiusSteps; iz++)
                        {
                            float radius = new Vector2(ix, iz).magnitude * horizontalStep;
                            if (radius > scanRadius) continue;

                            var position = new Vector3(origin.x + ix * horizontalStep, worldY, origin.z + iz * horizontalStep);
                            if (!TrySolve(arm, solver, position)) continue;

                            count++;
                            min = Mathf.Min(min, radius);
                            max = Mathf.Max(max, radius);
                            WorkspaceSamples.Add(new Sample { Position = position, Reachable = true, Color = color });
                        }
                    }

                    levels.Add((worldY, count, min, max));
                    string row = count > 0 ? $"수평 {min:0.00} ~ {max:0.00}m ({count}칸)" : "닿는 곳 없음";
                    log.AppendLine($"  베이스 기준 {relative:+0.00;-0.00;0.00}m (월드 y={worldY:0.00}): {row}");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AddTaskRing(levels, origin, LidGraspHeight(out _, out _), new Color(0.1f, 0.85f, 0.2f), "뚜껑 잡는 높이", log);
            AddTaskRing(levels, origin, CoverHeight(out _), new Color(0.2f, 0.45f, 1f), "키트 박스 덮는 높이", log);

            Debug.Log(log.ToString());

            SceneView.duringSceneGui -= DrawWorkspace;
            SceneView.duringSceneGui += DrawWorkspace;
            SceneView.RepaintAll();
        }

        // 작업 높이에 가장 가까운 층(5cm 이내)의 안쪽/바깥쪽 반경을 링으로 남긴다. 테이블과 박스는 이 띠 안에 둔다.
        private static void AddTaskRing(List<(float worldY, int count, float min, float max)> levels, Vector3 origin, float worldY, Color color, string label, System.Text.StringBuilder log)
        {
            int best = -1;
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i].count == 0) continue;
                if (Mathf.Abs(levels[i].worldY - worldY) > 0.025f) continue;
                if (best < 0 || Mathf.Abs(levels[i].worldY - worldY) < Mathf.Abs(levels[best].worldY - worldY)) best = i;
            }

            if (best < 0)
            {
                log.AppendLine($"[ArmReachMap] {label} y={worldY:0.00}: 이 높이에는 닿는 곳이 없습니다. 받침대나 테이블 높이를 바꾸세요.");
                return;
            }

            var level = levels[best];
            WorkspaceRings.Add(new Ring { Center = new Vector3(origin.x, worldY, origin.z), MinRadius = level.min, MaxRadius = level.max, Color = color });
            log.AppendLine($"[ArmReachMap] {label} y={worldY:0.00}: 베이스에서 수평 {level.min:0.00} ~ {level.max:0.00}m 띠 안에 물건을 둔다 (가까운 층 y={level.worldY:0.00})");
        }

        private static void DrawWorkspace(SceneView view)
        {
            foreach (Sample s in WorkspaceSamples)
            {
                Handles.color = new Color(s.Color.r, s.Color.g, s.Color.b, 0.55f);
                Handles.SphereHandleCap(0, s.Position, Quaternion.identity, 0.03f, EventType.Repaint);
            }

            foreach (Ring ring in WorkspaceRings)
            {
                Handles.color = ring.Color;
                Handles.DrawWireDisc(ring.Center, Vector3.up, ring.MinRadius, 3f);
                Handles.DrawWireDisc(ring.Center, Vector3.up, ring.MaxRadius, 3f);
            }
        }

        // 씬에는 FR5 등 다른 팔도 있으므로 UR3 프로필을 쓰는 팔(로봇암 B)을 고른다.
        private static ArmController FindArmB()
        {
            foreach (ArmController candidate in Object.FindObjectsByType<ArmController>(FindObjectsInactive.Exclude))
            {
                if (candidate.Profile != null && candidate.Profile.name.Contains("UR3"))
                    return candidate;
            }

            return null;
        }

        private static bool TrySolveFirstYaw(ArmController arm, IInverseKinematics solver, Vector3 worldPosition)
        {
            float[] seed = (float[])arm.HomePoseRad.Clone();
            float[] result = new float[seed.Length];
            Transform baseLink = arm.BaseLink;
            Quaternion worldRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            var targetInBase = new Pose(
                baseLink.InverseTransformPoint(worldPosition),
                Quaternion.Inverse(baseLink.rotation) * worldRotation);
            return solver.Solve(targetInBase, seed, result).Converged;
        }

        [MenuItem("Tools/MediKit/Clear ArmB Reach Map")]
        public static void Clear()
        {
            Samples.Clear();
            WorkspaceSamples.Clear();
            WorkspaceRings.Clear();
            SceneView.duringSceneGui -= Draw;
            SceneView.duringSceneGui -= DrawWorkspace;
            SceneView.RepaintAll();
        }

        // 뚜껑을 잡는 높이는 맨 위 뚜껑의 월드 높이이다. 씬에 없으면 기본값(0.639m)을 쓴다.
        private static float LidGraspHeight(out Vector3 lidCenter, out bool found)
        {
            GameObject lid = FindActive("Lid_01");
            found = lid != null;
            lidCenter = found ? lid.transform.position : Vector3.zero;
            return found ? lidCenter.y : 0.639f;
        }

        // 덮는 높이는 컨베이어 위 키트 박스 윗면에 뚜껑 윗판이 닿는 높이이다. 기준은 패킹 센서(Sensor_Packing)의 컨베이어 윗면이다.
        private static float CoverHeight(out GameObject sensor)
        {
            sensor = FindActive("Sensor_Packing");
            float beltY = sensor != null ? sensor.transform.position.y : 0.91f;
            return beltY + 0.135f + CoverClearance;
        }

        // Sensor_Packing 아래에서 이름에 KitBox가 들어간 오브젝트를 모두 찾는다. 이름이 바뀌어도 KitBox가 들어 있으면 잡힌다.
        // 찾은 오브젝트의 자식은 따로 세지 않는다(박스 안의 부품이 같은 이름을 가져도 한 번만 센다).
        private static List<Transform> FindKitBoxes(Transform root)
        {
            var found = new List<Transform>();
            CollectKitBoxes(root, found);
            return found;
        }

        private static void CollectKitBoxes(Transform parent, List<Transform> found)
        {
            foreach (Transform child in parent)
            {
                if (child.name.IndexOf("KitBox", System.StringComparison.OrdinalIgnoreCase) >= 0) found.Add(child);
                else CollectKitBoxes(child, found);
            }
        }

        private struct Range2
        {
            public bool Any;
            public float MinRadius;
            public float MaxRadius;

            public override string ToString()
            {
                return Any ? $"베이스에서 수평 {MinRadius:0.00} ~ {MaxRadius:0.00}m" : "닿는 곳 없음";
            }
        }

        private static void ScanLevel(ArmController arm, IInverseKinematics solver, Vector3 origin, float worldY, Color color, out Range2 range, out int count)
        {
            range = new Range2 { MinRadius = float.MaxValue, MaxRadius = 0f };
            count = 0;

            int steps = Mathf.RoundToInt(Range / Step);
            for (int ix = -steps; ix <= steps; ix++)
            {
                for (int iz = -steps; iz <= steps; iz++)
                {
                    Vector3 position = new Vector3(origin.x + ix * Step, worldY, origin.z + iz * Step);
                    bool reachable = TrySolve(arm, solver, position);
                    Samples.Add(new Sample { Position = position, Reachable = reachable, Color = color });
                    if (!reachable) continue;

                    count++;
                    float radius = new Vector2(position.x - origin.x, position.z - origin.z).magnitude;
                    range.Any = true;
                    range.MinRadius = Mathf.Min(range.MinRadius, radius);
                    range.MaxRadius = Mathf.Max(range.MaxRadius, radius);
                }
            }
        }

        // 그리퍼를 아래로 향한 채 position에 닿는지 푼다. 그리퍼 방향을 Yaws만큼 돌려 보고 하나라도 풀리면 true이다.
        private static bool TrySolve(ArmController arm, IInverseKinematics solver, Vector3 worldPosition)
        {
            float[] seed = (float[])arm.HomePoseRad.Clone();
            float[] result = new float[seed.Length];
            Transform baseLink = arm.BaseLink;

            foreach (float yaw in Yaws)
            {
                Vector3 up = Quaternion.AngleAxis(yaw, Vector3.up) * Vector3.forward;
                Quaternion worldRotation = Quaternion.LookRotation(Vector3.down, up);
                var targetInBase = new Pose(
                    baseLink.InverseTransformPoint(worldPosition),
                    Quaternion.Inverse(baseLink.rotation) * worldRotation);

                System.Array.Copy(arm.HomePoseRad, seed, seed.Length);
                if (solver.Solve(targetInBase, seed, result).Converged) return true;
            }

            return false;
        }

        // 맨 위 뚜껑의 네 테두리 가운데 점(그리퍼가 집을 곳)이 닿는지 적는다.
        private static void ReportLidEdges(ArmController arm, IInverseKinematics solver, Vector3 lidCenter)
        {
            // 뚜껑 바깥 크기 0.47 x 0.37m의 가장자리 점. 테두리 윗부분을 집으므로 높이는 뚜껑 중심이다.
            var edges = new[]
            {
                ("+X 가장자리", lidCenter + new Vector3(0.235f, 0f, 0f)),
                ("-X 가장자리", lidCenter + new Vector3(-0.235f, 0f, 0f)),
                ("+Z 가장자리", lidCenter + new Vector3(0f, 0f, 0.185f)),
                ("-Z 가장자리", lidCenter + new Vector3(0f, 0f, -0.185f)),
            };

            // 네 곳을 모두 적지만 실제로 집는 곳은 팔 쪽 가장자리 하나이므로 가까운 쪽만 닿으면 된다.
            foreach (var (label, point) in edges) ReportPoint(arm, solver, "뚜껑 집기 " + label, point);
        }

        private static void ReportPoint(ArmController arm, IInverseKinematics solver, string label, Vector3 worldPosition)
        {
            bool reachable = TrySolve(arm, solver, worldPosition);
            float horizontal = new Vector2(worldPosition.x - arm.BaseLink.position.x, worldPosition.z - arm.BaseLink.position.z).magnitude;
            string message = $"[ArmReachMap] {label}: {(reachable ? "닿음" : "닿지 않음")} (베이스에서 수평 {horizontal:0.00}m, y={worldPosition.y:0.00})";
            if (reachable) Debug.Log(message);
            else Debug.LogWarning(message);
        }

        private static GameObject FindActive(string objectName)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == objectName && t.gameObject.scene.IsValid()) return t.gameObject;
            }

            return null;
        }

        private static void Draw(SceneView view)
        {
            foreach (Sample s in Samples)
            {
                Handles.color = s.Reachable ? new Color(s.Color.r, s.Color.g, s.Color.b, 0.8f) : new Color(1f, 0.3f, 0.3f, 0.12f);
                float size = s.Reachable ? 0.02f : 0.008f;
                Handles.SphereHandleCap(0, s.Position, Quaternion.identity, size * 2f, EventType.Repaint);
            }
        }
    }
}
