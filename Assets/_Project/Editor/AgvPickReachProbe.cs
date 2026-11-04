using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// UnloaderRobot(robot_boxy)가 Load/Unload 애니메이션에서 박스를 잡거나 놓는 순간의 팔 위치를 재서 Console에 남긴다.
    /// 클립의 AttachBoxToRobot / DetachBoxToShelf 이벤트 시각의 자세를 SampleAnimation으로 만들어 읽고, 끝나면 원래 자세로 되돌린다.
    /// 재생 모드가 필요 없다. 씬은 바꾸지 않는다(자세를 되돌리지만 씬이 더러워진 표시가 뜰 수 있으니 저장하지 않는다).
    /// 좌표는 Animator가 붙은 오브젝트의 로컬 좌표이다. 로봇이 적재 방향으로 향하는 축이 어느 것인지 함께 출력한다.
    /// </summary>
    public static class AgvPickReachProbe
    {
        private const float MovedThresholdM = 0.01f;
        private const int MaxMovers = 6;

        [MenuItem("Tools/MediKit/Measure AGV Pick Reach")]
        public static void Measure()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[AgvPickReachProbe] 재생 모드를 끄고 실행하세요.");
                return;
            }

            Transform robot = FindByName("UnloaderRobot");
            Animator animator = robot != null ? robot.GetComponentInChildren<Animator>(true) : null;
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Debug.LogError("[AgvPickReachProbe] 열린 씬에 Animator가 있는 UnloaderRobot이 있어야 합니다. Scene_Cell을 연 뒤 다시 실행하세요.");
                return;
            }

            GameObject root = animator.gameObject;
            Transform frame = animator.transform;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            var saved = all.Select(t => (t.localPosition, t.localRotation, t.localScale)).ToArray();

            AnimationClip idle = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => c.name == "Idle");
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips
                .Where(c => c.name.StartsWith("Load_Level") || c.name.StartsWith("Unload_Level"))
                .OrderBy(c => c.name)
                .ToArray();

            var sb = new StringBuilder();
            sb.AppendLine($"[AgvPickReachProbe] 기준 오브젝트 '{frame.name}' 위치 {Fmt(frame.position)}, yaw {frame.eulerAngles.y:F1}도");
            sb.AppendLine($"  기준의 월드 축: 로컬 +X→{Fmt(frame.right)}, +Y→{Fmt(frame.up)}, +Z→{Fmt(frame.forward)}  (이하 좌표는 모두 기준 오브젝트의 로컬)");

            Transform pointBox = all.FirstOrDefault(t => t.name == "Point_box");
            if (pointBox != null)
            {
                sb.AppendLine($"  Point_box(들고 이동할 때 박스 위치): {Fmt(ToFrame(frame, pointBox.position))}");
            }

            try
            {
                foreach (AnimationClip clip in clips)
                {
                    AppendClip(sb, clip, idle, root, frame, all);
                }
            }
            finally
            {
                for (int i = 0; i < all.Length; i++)
                {
                    all[i].localPosition = saved[i].localPosition;
                    all[i].localRotation = saved[i].localRotation;
                    all[i].localScale = saved[i].localScale;
                }
            }

            Debug.Log(sb.ToString());
        }

        private static void AppendClip(StringBuilder sb, AnimationClip clip, AnimationClip idle, GameObject root, Transform frame, Transform[] all)
        {
            AnimationEvent[] events = clip.events
                .Where(e => e.functionName == "AttachBoxToRobot" || e.functionName == "DetachBoxToShelf")
                .ToArray();
            if (events.Length == 0)
            {
                sb.AppendLine($"{clip.name}: 박스를 붙이거나 떼는 이벤트가 없음");
                return;
            }

            (idle ?? clip).SampleAnimation(root, 0f);
            Vector3[] start = all.Select(t => ToFrame(frame, t.position)).ToArray();

            foreach (AnimationEvent evt in events)
            {
                clip.SampleAnimation(root, evt.time);
                var movers = new List<(Transform t, float d)>();
                for (int i = 0; i < all.Length; i++)
                {
                    float d = Vector3.Distance(start[i], ToFrame(frame, all[i].position));
                    if (d > MovedThresholdM)
                    {
                        movers.Add((all[i], d));
                    }
                }

                sb.AppendLine($"{clip.name} / {evt.functionName} @ {evt.time:F3}초 (클립 길이 {clip.length:F3}초): Idle 자세에서 {MovedThresholdM * 100:F0}cm 넘게 움직인 오브젝트 {movers.Count}개");

                Bounds? bounds = null;
                foreach ((Transform t, float d) in movers)
                {
                    foreach (Renderer renderer in t.GetComponents<Renderer>())
                    {
                        bounds = Encapsulate(bounds, frame, renderer.bounds);
                    }
                }

                if (bounds.HasValue)
                {
                    sb.AppendLine($"  움직인 부위 모델 범위(기준 로컬): min {Fmt(bounds.Value.min)} max {Fmt(bounds.Value.max)}");
                }

                foreach ((Transform t, float d) in movers.OrderByDescending(m => m.d).Take(MaxMovers))
                {
                    Vector3 local = ToFrame(frame, t.position);
                    sb.AppendLine($"  - {PathOf(t, frame)}: 위치 {Fmt(local)}, Idle 대비 {d:F3}m 이동");
                }
            }
        }

        private static Bounds? Encapsulate(Bounds? current, Transform frame, Bounds worldBounds)
        {
            Vector3 c = worldBounds.center;
            Vector3 e = worldBounds.extents;
            Bounds result = current ?? new Bounds(ToFrame(frame, c), Vector3.zero);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    for (int sz = -1; sz <= 1; sz += 2)
                    {
                        result.Encapsulate(ToFrame(frame, c + new Vector3(sx * e.x, sy * e.y, sz * e.z)));
                    }
                }
            }

            return result;
        }

        private static string PathOf(Transform t, Transform stop)
        {
            var names = new List<string>();
            for (Transform cur = t; cur != null && cur != stop; cur = cur.parent)
            {
                names.Add(cur.name);
            }

            names.Reverse();
            return string.Join("/", names);
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

        // 월드 좌표를 기준 오브젝트의 위치·회전만으로 바꾼다. 스케일은 곱하지 않아 미터 단위가 그대로 남는다.
        // (InverseTransformPoint는 기준의 스케일로 나누어, 기준을 줄여도 값이 변하지 않아 줄인 효과를 볼 수 없다.)
        private static Vector3 ToFrame(Transform frame, Vector3 worldPoint)
        {
            return Quaternion.Inverse(frame.rotation) * (worldPoint - frame.position);
        }

        private static string Fmt(Vector3 v) => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
    }
}
