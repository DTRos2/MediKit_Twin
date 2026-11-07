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

        // 그리퍼가 최대로 벌어지는 폭(m). 뚜껑 x 폭이 이 안에 들어와야 쥘 수 있다.
        private const float GripperMaxOpening = 0.14f;

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

            if (hasLid) ReportLidGrasp(arm, solver, lidCenter);
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
                    // 그리퍼는 뚜껑 중심에서 x 방향 양면을 쥐고 있으므로, 덮을 때 그리퍼 끝은 박스 중심 바로 위에 온다.
                    ReportPoint(arm, solver, $"키트 박스 위에 덮기, {kitBox.name} 중심(그리퍼 끝)", coverCenter);

                    // 뚜껑이 박스에 실제로 앉았을 때의 높이(박스 윗면 기준). 위의 덮는 높이보다 낮으므로 따로 확인한다.
                    float seatedY = MediKitTwin.ArmB.LidCoverPlanner.CoverHeight(TopOf(kitBox), CellSceneBuilder.LidHeight, CellSceneBuilder.LidWall);
                    ReportPoint(arm, solver, $"키트 박스에 뚜껑이 앉은 높이, {kitBox.name} 중심(그리퍼 끝)", new Vector3(kitBox.position.x, seatedY, kitBox.position.z));
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

        // 팔이 멈춰 있을 때, 팔 링크의 충돌체와 겹쳐 있는 주변 충돌체를 이름과 겹친 깊이로 알려 준다. 무엇에 걸렸는지 찾는 용도다.
        // 받침대와 베이스는 원래 붙어 있으므로 겹침으로 나와도 정상이다.
        [MenuItem("Tools/MediKit/Report ArmB Contacts")]
        public static void ReportContacts()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[ArmReachMap] 플레이 모드에서 실행하세요.");
                return;
            }

            ArmController arm = FindArmB();
            if (arm == null)
            {
                Debug.LogError("[ArmReachMap] UR3 프로필을 쓰는 ArmController를 찾지 못했습니다.");
                return;
            }

            Transform root = arm.RobotRoot;
            var seen = new HashSet<(Collider, Collider)>();
            var log = new System.Text.StringBuilder();
            int count = 0;

            foreach (Collider own in root.GetComponentsInChildren<Collider>())
            {
                if (!own.enabled || own.isTrigger) continue;

                Bounds bounds = own.bounds;
                foreach (Collider other in Physics.OverlapBox(bounds.center, bounds.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (other == own || other.transform.IsChildOf(root)) continue;
                    if (Physics.GetIgnoreLayerCollision(own.gameObject.layer, other.gameObject.layer)) continue;

                    if (!seen.Add((own, other))) continue;

                    string detail;
                    if (IsNonConvexMesh(own) || IsNonConvexMesh(other))
                    {
                        detail = "경계 상자만 겹침(정밀 판정 불가)";
                    }
                    else if (Physics.ComputePenetration(own, own.transform.position, own.transform.rotation,
                                 other, other.transform.position, other.transform.rotation, out Vector3 direction, out float depth))
                    {
                        detail = $"겹침 깊이 {depth * 100f:0.0}cm";
                    }
                    else
                    {
                        continue;
                    }

                    count++;
                    log.AppendLine($"  {PathOf(own.transform, root)}  <->  {PathOf(other.transform, null)}  : {detail}");
                }
            }

            Debug.Log(count == 0
                ? "[ArmReachMap] 팔 링크와 겹친 주변 충돌체가 없습니다."
                : $"[ArmReachMap] 팔 링크와 겹친 충돌체 {count}개 (받침대, 베이스는 정상):\n{log}");
        }

        // 팔이 지금 있는 자세를 UR3 프로필의 시작 자세(_homePoseDeg)로 저장한다. 프로필은 팀원과 공유하는 에셋이라 확인 창을 띄운다.
        // 팔을 원하는 자세로 두고(Follow Target으로 옮기거나 Follow Target을 비워 멈춘 상태) 다 움직인 뒤에 누른다.
        [MenuItem("Tools/MediKit/Save ArmB Current Pose As Home Pose")]
        public static void SaveCurrentPoseAsHome()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[ArmReachMap] 플레이 모드에서 실행하세요. (현재 관절 각도를 읽어야 합니다)");
                return;
            }

            ArmController arm = FindArmB();
            if (arm == null || !arm.IsReady)
            {
                Debug.LogError("[ArmReachMap] UR3 프로필을 쓰는 ArmController가 없거나 아직 준비되지 않았습니다.");
                return;
            }

            int count = arm.JointNames.Count;
            var angles = new float[count];
            arm.GetJointPositions(angles);

            var degrees = new float[count];
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++)
            {
                degrees[i] = Mathf.Round(angles[i] * Mathf.Rad2Deg * 10f) / 10f;
                text.Append($"{degrees[i]:0.#}  ");
            }

            string settled = arm.IsAtTarget(0.03f) ? "" : "\n\n주의: 팔이 아직 목표 각도에 도달하지 않았습니다(움직이는 중이거나 막혀 있음).";
            bool ok = EditorUtility.DisplayDialog(
                "ArmB 시작 자세 저장",
                $"'{arm.Profile.name}'의 시작 자세(_homePoseDeg)를 현재 자세로 바꿉니다.\n\n관절 각도(도): {text}\n\n이 프로필은 팀원과 공유하는 에셋입니다.{settled}",
                "저장", "취소");
            if (!ok) return;

            var serialized = new SerializedObject(arm.Profile);
            SerializedProperty home = serialized.FindProperty("_homePoseDeg");
            home.arraySize = count;
            for (int i = 0; i < count; i++) home.GetArrayElementAtIndex(i).floatValue = degrees[i];
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(arm.Profile);
            AssetDatabase.SaveAssets();

            Debug.Log($"[ArmReachMap] '{arm.Profile.name}'의 시작 자세를 저장했습니다(도): {text}");
        }

        private static bool IsNonConvexMesh(Collider collider)
        {
            return collider is MeshCollider mesh && !mesh.convex;
        }

        private static string PathOf(Transform t, Transform stopAt)
        {
            string path = t.name;
            while (t.parent != null && t.parent != stopAt)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }

        // IkArmTarget의 Follow Target이 지금 있는 자세에 팔이 닿는지 알려 준다. 팔이 안 따라올 때 위치 문제인지 방향 문제인지 가른다.
        [MenuItem("Tools/MediKit/Check ArmB Follow Target Reach")]
        public static void CheckFollowTarget()
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

            IkArmTarget ik = arm.GetComponent<IkArmTarget>();
            var follow = ik != null ? new SerializedObject(ik).FindProperty("_followTarget").objectReferenceValue as Transform : null;
            if (follow == null)
            {
                Debug.LogError("[ArmReachMap] ArmB의 IkArmTarget에 Follow Target이 연결돼 있지 않습니다.");
                return;
            }

            DlsIkSettings settings = arm.Profile.Ik;
            IInverseKinematics solver = new MultiStartInverseKinematics(
                new DlsInverseKinematics(arm.Chain, settings), arm.Chain, settings.Restarts, settings.RestartSeed);

            Vector3 position = follow.position;
            Vector3 fromBase = position - arm.BaseLink.position;
            float horizontal = new Vector2(fromBase.x, fromBase.z).magnitude;
            float tilt = Vector3.Angle(follow.forward, Vector3.down);

            float[] seed = (float[])arm.HomePoseRad.Clone();
            float[] result = new float[seed.Length];
            var targetInBase = new Pose(
                arm.BaseLink.InverseTransformPoint(position),
                Quaternion.Inverse(arm.BaseLink.rotation) * follow.rotation);
            bool asPlaced = solver.Solve(targetInBase, seed, result).Converged;
            bool pointingDown = TrySolve(arm, solver, position);

            // IkArmTarget은 홈 자세가 아니라 지금 관절 각도에서 출발해 푼다. 같은 조건으로도 풀어 본다.
            var current = new float[seed.Length];
            arm.GetJointPositions(current);
            bool fromCurrent = solver.Solve(targetInBase, current, result).Converged;

            Pose tool = arm.ToolPose;
            float gap = Vector3.Distance(tool.position, position);
            var degrees = new System.Text.StringBuilder();
            for (int i = 0; i < current.Length; i++) degrees.Append($"{current[i] * Mathf.Rad2Deg:0} ");

            Debug.Log($"[ArmReachMap] IkArmTarget 상태: 목표 있음={ik.HasToolTarget}, 마지막 풀이 성공={ik.LastSolveSucceeded}, Move When Unreachable={new SerializedObject(ik).FindProperty("_moveWhenUnreachable").boolValue}");
            Debug.Log($"[ArmReachMap] 현재 그리퍼 끝점 {tool.position}, 목표까지 {gap:0.00}m, 관절 각도(도) {degrees}");
            Debug.Log($"[ArmReachMap] 지금 관절 각도에서 출발해 풀면: {(fromCurrent ? "풀림" : "안 풀림")}");

            // 팔이 목표 관절 각도까지 못 가고 있는지(막힘) 아직 가는 중인지 본다. 몇 초 간격으로 두 번 눌러 비교한다.
            var goal = new float[seed.Length];
            var velocity = new float[seed.Length];
            arm.GetJointVelocities(velocity);
            var goalText = new System.Text.StringBuilder();
            var errorText = new System.Text.StringBuilder();
            float maxVelocity = 0f;
            if (ik.TryGetTargets(goal))
            {
                for (int i = 0; i < goal.Length; i++)
                {
                    goalText.Append($"{goal[i] * Mathf.Rad2Deg:0} ");
                    errorText.Append($"{(goal[i] - current[i]) * Mathf.Rad2Deg:+0;-0;0} ");
                }
            }
            else goalText.Append("(IkArmTarget이 목표 각도를 주지 않음)");

            for (int i = 0; i < velocity.Length; i++) maxVelocity = Mathf.Max(maxVelocity, Mathf.Abs(velocity[i]));
            Debug.Log($"[ArmReachMap] 목표 관절 각도(도) {goalText}");
            Debug.Log($"[ArmReachMap] 관절별 남은 각도(도) {errorText} / 가장 빠른 관절 속도 {maxVelocity * Mathf.Rad2Deg:0.0}도/초 / 목표 도달={arm.IsAtTarget(0.03f)}");

            Debug.Log($"[ArmReachMap] {follow.name}: 위치 {position}, 베이스에서 수평 {horizontal:0.00}m, 베이스 기준 높이 {fromBase.y:+0.00;-0.00}m, 파란 축이 아래에서 {tilt:0}° 기울어짐");
            Debug.Log($"[ArmReachMap] 지금 방향 그대로: {(asPlaced ? "닿음" : "닿지 않음")} / 그리퍼를 아래로 향하면(방향 4가지 시도): {(pointingDown ? "닿음" : "닿지 않음")}");

            if (!asPlaced && pointingDown) Debug.LogWarning("[ArmReachMap] 위치는 닿지만 지금 방향으로는 안 닿습니다. 회전을 X=90, Y=0, Z=0 근처로 맞추세요.");
            else if (!asPlaced && !pointingDown) Debug.LogWarning("[ArmReachMap] 이 위치에는 어떤 방향으로도 닿지 않습니다. 팔 쪽으로 더 가깝게 또는 높이를 바꾸세요.");
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

        // 그리퍼가 열린 상태에서 두 손가락 패드 사이 실제 간격을 잰다. 뚜껑 폭이 이 안에 들어가는지 확인하는 용도이다.
        // 손가락이 닫히는 방향은 도구 X, 접근 방향은 도구 +Z이다. 패드는 이름에 inner_finger_pad가 들어간 콜라이더로 찾는다.
        [MenuItem("Tools/MediKit/Measure ArmB Gripper Gap")]
        public static void MeasureGripperGap()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("[ArmReachMap] 플레이 모드에서 그리퍼가 열린 상태로 실행하세요.");
                return;
            }

            ArmController arm = FindArmB();
            if (arm == null || !arm.IsReady)
            {
                Debug.LogError("[ArmReachMap] UR3 프로필을 쓰는 ArmController를 찾지 못했거나 아직 준비되지 않았습니다.");
                return;
            }

            Pose tool = arm.ToolPose;
            Vector3 closeAxis = tool.rotation * Vector3.right;
            Vector3 approachAxis = tool.rotation * Vector3.forward;

            var report = new System.Text.StringBuilder($"[ArmReachMap] 그리퍼 패드 (도구 X = 닫히는 방향, 도구 Z = 접근 방향, TCP 기준 m)");
            var ranges = new List<(float min, float max)>();
            foreach (Collider collider in arm.RobotRoot.GetComponentsInChildren<Collider>())
            {
                string path = PathOf(collider.transform, arm.RobotRoot);
                if (path.IndexOf("inner_finger_pad", System.StringComparison.OrdinalIgnoreCase) < 0) continue;

                float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
                foreach (Vector3 corner in ColliderCorners(collider))
                {
                    Vector3 offset = corner - tool.position;
                    float x = Vector3.Dot(offset, closeAxis);
                    float z = Vector3.Dot(offset, approachAxis);
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                    minZ = Mathf.Min(minZ, z); maxZ = Mathf.Max(maxZ, z);
                }

                ranges.Add((minX, maxX));
                string side = path.Contains("left") ? "왼쪽" : path.Contains("right") ? "오른쪽" : "?";
                report.Append($"\n  {side} 패드: 닫히는 방향 {minX:0.0000} ~ {maxX:0.0000}, 접근 방향 {minZ:0.0000} ~ {maxZ:0.0000}");
            }

            if (ranges.Count != 2)
            {
                Debug.LogWarning(report + $"\n  패드 콜라이더가 {ranges.Count}개 잡혔습니다(2개여야 함). 이름 검색을 확인하세요.");
                return;
            }

            ranges.Sort((a, b) => a.min.CompareTo(b.min));
            float gap = ranges[1].min - ranges[0].max;
            float lidWidth = CellSceneBuilder.LidSize.x;
            report.Append($"\n  패드 사이 간격 {gap:0.0000}m, 뚜껑 폭 {lidWidth:0.0000}m → 한쪽 여유 {(gap - lidWidth) * 0.5f * 1000f:0.0}mm");
            report.Append($"\n  양쪽 패드의 TCP 기준 가운데 {(ranges[0].max + ranges[1].min) * 0.5f * 1000f:0.0}mm (0이면 TCP가 두 패드 한가운데)");
            if (gap > lidWidth) Debug.Log(report.ToString());
            else Debug.LogWarning(report + "\n  패드 간격이 뚜껑 폭보다 좁아 뚜껑을 감싸 쥘 수 없습니다.");
        }

        // 팔 끝점에서 가장 가까운 뚜껑(Lid_*)과 두 손가락 패드의 실제 위치 관계를 적는다. 어느 단계에서든 플레이 중에 실행한다.
        // 닫히는 방향(도구 X)으로 패드 안쪽 면과 뚜껑 옆면 사이 간격, 접근 방향(도구 Z)으로 패드 하단이 뚜껑 윗면·아랫면 대비 어디인지,
        // 뚜껑 중심이 TCP에서 얼마나 떨어졌는지, 두 패드 사이 간격(그리퍼가 열렸는지)을 보여 준다.
        [MenuItem("Tools/MediKit/Report ArmB Pad-Lid Gap")]
        public static void ReportPadLidGap()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("[ArmReachMap] 플레이 모드에서 실행하세요.");
                return;
            }

            ArmController arm = FindArmB();
            if (arm == null || !arm.IsReady)
            {
                Debug.LogError("[ArmReachMap] UR3 프로필을 쓰는 ArmController를 찾지 못했거나 아직 준비되지 않았습니다.");
                return;
            }

            Pose tool = arm.ToolPose;
            Vector3 closeAxis = tool.rotation * Vector3.right;
            Vector3 longAxis = tool.rotation * Vector3.up;
            Vector3 approachAxis = tool.rotation * Vector3.forward;

            Transform lid = null;
            float nearest = float.MaxValue;
            foreach (Transform candidate in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!candidate.name.StartsWith(CellObjectNames.LidPrefix) || !candidate.gameObject.scene.IsValid()) continue;
                float distance = (candidate.position - tool.position).sqrMagnitude;
                if (distance < nearest) { nearest = distance; lid = candidate; }
            }

            if (lid == null)
            {
                Debug.LogWarning("[ArmReachMap] Lid_로 시작하는 뚜껑을 찾지 못했습니다.");
                return;
            }

            var negative = new List<Collider>();
            var positive = new List<Collider>();
            foreach (Collider collider in arm.RobotRoot.GetComponentsInChildren<Collider>())
            {
                if (PathOf(collider.transform, arm.RobotRoot).IndexOf("inner_finger_pad", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                (Vector3.Dot(collider.bounds.center - tool.position, closeAxis) < 0f ? negative : positive).Add(collider);
            }

            Collider[] lidColliders = lid.GetComponentsInChildren<Collider>();
            if (negative.Count == 0 || positive.Count == 0
                || !ProjectRange(lidColliders, closeAxis, out float lidMin, out float lidMax)
                || !ProjectRange(lidColliders, approachAxis, out float lidTop, out float lidBottom)
                || !ProjectRange(negative, closeAxis, out _, out float negativeInner)
                || !ProjectRange(positive, closeAxis, out float positiveInner, out _)
                || !ProjectRange(negative, approachAxis, out _, out float negativeBottom)
                || !ProjectRange(positive, approachAxis, out _, out float positiveBottom))
            {
                Debug.LogWarning("[ArmReachMap] 패드나 뚜껑 콜라이더를 찾지 못했습니다.");
                return;
            }

            Vector3 offset = lid.position - tool.position;
            var report = new System.Text.StringBuilder($"[ArmReachMap] 패드-뚜껑 위치 ({lid.name}, 부모 {(lid.parent != null ? lid.parent.name : "없음")}, 도구 축 기준 mm)");
            report.Append($"\n  패드 사이 간격 {(positiveInner - negativeInner) * 1000f:0.0} (열림이면 약 128.5), 뚜껑 폭 {(lidMax - lidMin) * 1000f:0.0}");
            report.Append($"\n  패드 안쪽 면과 뚜껑 옆면 사이(음수면 겹침): -쪽 {(lidMin - negativeInner) * 1000f:0.0}, +쪽 {(positiveInner - lidMax) * 1000f:0.0}");
            report.Append($"\n  뚜껑 중심이 TCP에서: 닫히는 방향 {Vector3.Dot(offset, closeAxis) * 1000f:0.0}, 긴 변 방향 {Vector3.Dot(offset, longAxis) * 1000f:0.0}, 접근(아래) 방향 {Vector3.Dot(offset, approachAxis) * 1000f:0.0}");
            report.Append($"\n  패드 하단이 뚜껑 윗면보다 {(Mathf.Min(negativeBottom, positiveBottom) - lidTop) * 1000f:0.0} 아래, 아랫면보다 {(Mathf.Min(negativeBottom, positiveBottom) - lidBottom) * 1000f:0.0} (음수면 아랫면 위)");
            Debug.Log(report.ToString());
        }

        // 콜라이더 모서리를 axis에 투영한 최소·최대
        private static bool ProjectRange(IEnumerable<Collider> colliders, Vector3 axis, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            bool any = false;
            foreach (Collider collider in colliders)
            {
                foreach (Vector3 corner in ColliderCorners(collider))
                {
                    float value = Vector3.Dot(corner, axis);
                    min = Mathf.Min(min, value);
                    max = Mathf.Max(max, value);
                    any = true;
                }
            }

            return any;
        }

        private static IEnumerable<Vector3> ColliderCorners(Collider collider)
        {
            if (collider is BoxCollider box)
            {
                Vector3 half = box.size * 0.5f;
                for (int i = 0; i < 8; i++)
                {
                    var local = new Vector3((i & 1) == 0 ? -half.x : half.x, (i & 2) == 0 ? -half.y : half.y, (i & 4) == 0 ? -half.z : half.z);
                    yield return box.transform.TransformPoint(box.center + local);
                }
            }
            else
            {
                Bounds bounds = collider.bounds;
                for (int i = 0; i < 8; i++)
                {
                    yield return bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                }
            }
        }

        // 로봇암 B의 회전 관절마다 물리 엔진이 실제로 쓰는 한계와 현재 각도를 적는다.
        // IK가 쓰는 한계(Measure 결과의 한계 열)와 다르면, 팔이 목표 각도에 가지 못하고 한계에서 멈춘 것이다.
        [MenuItem("Tools/MediKit/Report ArmB Joint Limits")]
        public static void ReportJointLimits()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("[ArmReachMap] 플레이 모드에서 실행하세요. (현재 관절 각도를 읽어야 합니다)");
                return;
            }

            ArmController arm = FindArmB();
            if (arm == null || !arm.IsReady)
            {
                Debug.LogError("[ArmReachMap] UR3 프로필을 쓰는 ArmController를 찾지 못했거나 아직 준비되지 않았습니다.");
                return;
            }

            var report = new System.Text.StringBuilder("[ArmReachMap] 회전 관절 한계(물리 엔진 기준, 도)");
            foreach (ArticulationBody body in arm.RobotRoot.GetComponentsInChildren<ArticulationBody>())
            {
                if (body.jointType != ArticulationJointType.RevoluteJoint || body.dofCount < 1) continue;

                ArticulationDrive drive = body.xDrive;
                float current = body.jointPosition[0] * Mathf.Rad2Deg;
                report.Append($"\n  {body.name}: 현재 {current:0.0}, 한계 [{drive.lowerLimit:0.0}, {drive.upperLimit:0.0}], 회전 잠금 {body.twistLock}, 구동 목표 {drive.target:0.0}, 강성 {drive.stiffness:0}, 힘 한계 {drive.forceLimit:0}");
            }

            Debug.Log(report.ToString());
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

        // 뚜껑을 잡는 높이는 맨 위 뚜껑의 월드 높이이다. 씬에 없으면 기본값(0.91m)을 쓴다.
        private static float LidGraspHeight(out Vector3 lidCenter, out bool found)
        {
            GameObject lid = FindActive("Lid_01");
            found = lid != null;
            lidCenter = found ? lid.transform.position : Vector3.zero;
            return found ? lidCenter.y : 0.91f;
        }

        // 덮는 높이는 컨베이어 위 키트 박스 윗면에 뚜껑 윗판이 닿는 높이이다. 기준은 패킹 센서(Sensor_Packing)의 컨베이어 윗면이다.
        private static float CoverHeight(out GameObject sensor)
        {
            sensor = FindActive(CellObjectNames.PackingSensor);
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

        // 박스의 벽과 바닥 렌더러를 모두 감싼 월드 최고 높이
        private static float TopOf(Transform box)
        {
            float top = box.position.y;
            foreach (Renderer renderer in box.GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, renderer.bounds.max.y);
            return top;
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

        // 맨 위 뚜껑을 집는 점이 닿는지 적는다. 그리퍼는 뚜껑 중심에서 x 방향 양면을 쥐므로(최대 벌림 0.14m 안에 들어오는 폭)
        // 집는 점은 뚜껑 중심이다. 뚜껑 크기는 뚜껑 자신의 축 기준으로 읽는다. 뚜껑이 월드에서 돌아 있어도 x는 그리퍼가 쥐는 폭이다.
        private static void ReportLidGrasp(ArmController arm, IInverseKinematics solver, Vector3 lidCenter)
        {
            GameObject lid = FindActive("Lid_01");
            if (lid != null && TryGetLocalSize(lid.transform, out Vector3 size))
            {
                string fit = size.x <= GripperMaxOpening ? "그리퍼 벌림 안에 들어옴" : "그리퍼 벌림보다 넓음";
                Debug.Log($"[ArmReachMap] 뚜껑 크기(뚜껑 축 기준) x {size.x:0.0000} · y {size.y:0.0000} · z {size.z:0.0000}m, 그리퍼 최대 벌림 {GripperMaxOpening:0.00}m: {fit} (여유 {(GripperMaxOpening - size.x) * 0.5f * 1000f:0.0}mm씩)");
                Debug.Log($"[ArmReachMap] 뚜껑 방향: 월드 yaw {lid.transform.eulerAngles.y:0.0}도, 긴 변(z)이 가리키는 월드 방향 {lid.transform.forward}");
            }

            ReportPoint(arm, solver, "뚜껑 집기 중심(그리퍼 끝)", lidCenter);
        }

        // 오브젝트 아래 모든 메시를 감싸는 크기를 그 오브젝트의 로컬 축 기준으로 잰다(월드 스케일 반영).
        private static bool TryGetLocalSize(Transform root, out Vector3 size)
        {
            Bounds bounds = default;
            bool any = false;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                Bounds mesh = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = mesh.center + Vector3.Scale(mesh.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 local = root.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if (!any) { bounds = new Bounds(local, Vector3.zero); any = true; }
                    else bounds.Encapsulate(local);
                }
            }

            size = Vector3.Scale(bounds.size, root.lossyScale);
            return any;
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
