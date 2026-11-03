using System.Text;
using ConveyorSystem;
using UnityEditor;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 푸셔, 컨베이어, 불량 박스의 배치를 재서 Console에 숫자로 남긴다. (FR-09) 아무것도 옮기거나 바꾸지 않는다.
    /// 열려 있는 씬(Scene_Cell)에서 푸셔 헤드가 밀 수 있는지, 밀려난 박스가 불량 박스에 닿는지를 숫자로 판단하기 위한 도구이다.
    /// 밀기 방향은 푸셔 오브젝트의 로컬 +X(CellSceneBuilder가 U자 안쪽에서 바깥으로 민다고 놓은 방향)로 본다.
    /// </summary>
    public static class PusherLayoutProbe
    {
        private const float Gravity = 9.81f;

        // 키트 박스 크기(m). 슬롯이 Z축을 따라 놓여 있어 밀기 방향(X)의 폭이 짧은 변이다.
        private static readonly Vector3 KitBoxSize = new Vector3(0.0976f, 0.10f, 0.2387f);

        [MenuItem("Tools/MediKit/Measure Pusher Layout")]
        public static void Measure()
        {
            Transform head = FindByName("PushHead");
            Transform bin = FindByName("Bin");
            if (head == null || bin == null)
            {
                Debug.LogError("[PusherLayoutProbe] 열려 있는 씬에 PushHead와 Bin이 있어야 합니다. Scene_Cell을 연 뒤 다시 실행하세요.");
                return;
            }

            Transform pusherRoot = head.root;
            Transform pusher = head.parent != null ? head.parent : head;
            Vector3 push = Flat(pusher.right);
            Vector3 side = Vector3.Cross(Vector3.up, push);

            ConveyorBase conveyor = FindNearestConveyor(head.position);
            if (conveyor == null)
            {
                Debug.LogError("[PusherLayoutProbe] 열려 있는 씬에 컨베이어가 없습니다.");
                return;
            }

            Bounds headBounds = BoundsOf(head.gameObject);
            Bounds convBounds = BoundsOf(conveyor.gameObject);
            Transform floor = bin.Find("Floor");
            Transform wallNear = FindWall(bin, push, true);
            Transform wallFar = FindWall(bin, push, false);
            Transform zone = FindByName("LoadZone");

            float beltTop = conveyor.CenterPoint.position.y;
            Vector3 belt = conveyor.CenterPoint.position;

            var sb = new StringBuilder();
            sb.AppendLine($"[PusherLayoutProbe] 밀기 방향 {Fmt(push)} (푸셔 '{pusher.name}' 로컬 +X)");

            // 밀기 방향 좌표 s, 옆 방향 좌표 t. 컨베이어 중심(CenterPoint)을 s=0, t=0으로 놓는다.
            sb.AppendLine($"컨베이어 '{conveyor.name}': 윗면 높이 y={beltTop:F3}, 중심 {Fmt(belt)}");
            sb.AppendLine($"  모델 범위 s {AlongExtent(convBounds, belt, push, max: false):F3} ~ {AlongExtent(convBounds, belt, push, max: true):F3} (밀기 방향), t {Extent(convBounds, belt, side, true):F3} ~ {Extent(convBounds, belt, side, false):F3} (옆), 높이 y {convBounds.min.y:F3} ~ {convBounds.max.y:F3}");

            sb.AppendLine($"푸셔 헤드 '{head.name}': 중심 s={Along(headBounds.center, belt, push):F3}, 크기 {Fmt(headBounds.size)}");
            sb.AppendLine($"  높이 y {headBounds.min.y:F3} ~ {headBounds.max.y:F3} (컨베이어 윗면 기준 {headBounds.min.y - beltTop:+0.000;-0.000} ~ {headBounds.max.y - beltTop:+0.000;-0.000})");
            float headFront = AlongExtent(headBounds, belt, push, max: true);
            sb.AppendLine($"  앞면(밀기 방향) s={headFront:F3}, 컨베이어 중심과의 거리 {-headFront:F3}m");

            if (floor != null && wallNear != null && wallFar != null)
            {
                float nearInner = Along(wallNear.position, belt, push) + HalfAlong(wallNear, push);
                float farInner = Along(wallFar.position, belt, push) - HalfAlong(wallFar, push);
                float floorTop = floor.position.y + floor.lossyScale.y * 0.5f;
                float rimTop = wallNear.position.y + wallNear.lossyScale.y * 0.5f;
                sb.AppendLine($"불량 박스 안쪽: s {nearInner:F3} ~ {farInner:F3} (폭 {farInner - nearInner:F3}m), 바닥 윗면 y={floorTop:F3}, 벽 윗면 y={rimTop:F3}");
                if (zone != null)
                {
                    Bounds zoneBounds = BoundsOf(zone.gameObject, collidersOnly: true);
                    sb.AppendLine($"  LoadZone: s {AlongExtent(zoneBounds, belt, push, max: false):F3} ~ {AlongExtent(zoneBounds, belt, push, max: true):F3}, 높이 y {zoneBounds.min.y:F3} ~ {zoneBounds.max.y:F3}");
                }

                float edge = AlongExtent(convBounds, belt, push, max: true);
                float gap = nearInner - edge;
                float dropToRim = beltTop - rimTop;
                float dropToFloor = beltTop - floorTop;
                float boxCenterToEdge = edge; // 박스 중심이 컨베이어 중심(s=0)에 있을 때 가장자리까지 가는 거리
                sb.AppendLine($"컨베이어 가장자리(밀기 방향) s={edge:F3} → 불량 박스 안쪽 벽까지 수평 틈 {gap:F3}m");
                sb.AppendLine($"높이 차: 컨베이어 윗면 - 벽 윗면 = {dropToRim:F3}m, 컨베이어 윗면 - 바닥 윗면 = {dropToFloor:F3}m");

                if (dropToRim > 0f)
                {
                    float tFall = Mathf.Sqrt(2f * dropToRim / Gravity);
                    // 박스가 가장자리를 넘는 순간 속도 v로 날아가 벽 윗면 높이에 이를 때까지 수평으로 간 거리가 틈 이상이어야 안에 들어간다. (박스 중심 기준, 박스 폭의 절반만큼은 벽 위에서 걸려도 된다)
                    float need = Mathf.Max(0f, gap + KitBoxSize.x * 0.5f);
                    sb.AppendLine($"  벽 윗면까지 낙하 {tFall:F2}s. 틈 {gap:F3}m(+박스 반폭 {KitBoxSize.x * 0.5f:F3}m)를 그 시간에 가려면 가장자리에서 속도 {need / tFall:F2}m/s 이상이 필요하다.");
                }
                else
                {
                    sb.AppendLine("  컨베이어 윗면이 불량 박스 벽 윗면보다 낮다. 박스가 벽을 넘어 떨어지지 않는다.");
                }

                // 헤드 앞면은 지금 s=headFront에 있고, 박스 중심이 가장자리에 닿을 때 앞면은 가장자리 + 박스 반폭에 있다. 전진 거리는 그 차이이다.
                float frontAtEdge = boxCenterToEdge + KitBoxSize.x * 0.5f;
                sb.AppendLine($"밀기 거리: 박스 뒷면 s={-KitBoxSize.x * 0.5f:F3}에 닿을 때까지 {-KitBoxSize.x * 0.5f - headFront:F3}m, 박스 중심이 가장자리 s={boxCenterToEdge:F3}에 올 때까지 헤드 앞면이 s={frontAtEdge:F3}로 간다 = 전진 {frontAtEdge - headFront:F3}m (지금 헤드 앞면 s={headFront:F3} 기준)");
            }
            else
            {
                sb.AppendLine("불량 박스의 Floor, 벽을 찾지 못했습니다.");
            }

            // 컨베이어의 물리 충돌체(트리거 제외). 컨베이어 윗면보다 높고 밀기 길을 가로막는 가이드 레일이 있는지 본다.
            sb.AppendLine("컨베이어의 충돌체(트리거 제외), 컨베이어 윗면 기준 높이:");
            foreach (Collider c in conveyor.GetComponentsInChildren<Collider>())
            {
                if (c.isTrigger)
                {
                    continue;
                }

                Bounds cb = c.bounds;
                sb.AppendLine($"  {c.name}: s {AlongExtent(cb, belt, push, max: false):F3} ~ {AlongExtent(cb, belt, push, max: true):F3}, t {Extent(cb, belt, side, true):F3} ~ {Extent(cb, belt, side, false):F3}, 높이 {cb.min.y - beltTop:+0.000;-0.000} ~ {cb.max.y - beltTop:+0.000;-0.000}");
            }

            sb.AppendLine($"푸셔 오브젝트 '{pusherRoot.name}'의 월드 위치 {Fmt(pusherRoot.position)}");
            Debug.Log(sb.ToString());
        }

        private static Transform FindByName(string name)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }

        private static ConveyorBase FindNearestConveyor(Vector3 from)
        {
            ConveyorBase best = null;
            float bestSqr = float.MaxValue;
            foreach (ConveyorBase c in Object.FindObjectsByType<ConveyorBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (c.CenterPoint == null)
                {
                    continue;
                }

                Vector3 d = c.CenterPoint.position - from;
                d.y = 0f;
                if (d.sqrMagnitude < bestSqr)
                {
                    bestSqr = d.sqrMagnitude;
                    best = c;
                }
            }

            return best;
        }

        // 밀기 방향으로 가까운 쪽(near)이나 먼 쪽 벽. 밀기 방향에 수직인 벽(Wall_*Z)은 건너뛴다.
        private static Transform FindWall(Transform bin, Vector3 push, bool near)
        {
            Transform best = null;
            float bestS = near ? float.MaxValue : float.MinValue;
            foreach (Transform t in bin)
            {
                // 밀기 방향 쪽에 놓인 벽만 본다. 상자 중심과 같은 높이로 옆에 선 벽(밀기 방향 위치가 중심과 같은 벽)은 건너뛴다.
                if (!t.name.StartsWith("Wall_") || Mathf.Abs(Vector3.Dot(t.position - bin.position, push)) < 0.1f)
                {
                    continue;
                }

                float s = Vector3.Dot(t.position, push);
                if (near ? s < bestS : s > bestS)
                {
                    bestS = s;
                    best = t;
                }
            }

            return best;
        }

        private static float HalfAlong(Transform t, Vector3 push)
        {
            Vector3 size = t.lossyScale;
            return (Mathf.Abs(Vector3.Dot(t.right, push)) * size.x + Mathf.Abs(Vector3.Dot(t.up, push)) * size.y + Mathf.Abs(Vector3.Dot(t.forward, push)) * size.z) * 0.5f;
        }

        private static Bounds BoundsOf(GameObject go, bool collidersOnly = false)
        {
            bool has = false;
            var bounds = new Bounds(go.transform.position, Vector3.zero);
            if (collidersOnly)
            {
                foreach (Collider c in go.GetComponentsInChildren<Collider>())
                {
                    Encapsulate(ref bounds, ref has, c.bounds);
                }
            }
            else
            {
                foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
                {
                    Encapsulate(ref bounds, ref has, r.bounds);
                }
            }

            return bounds;
        }

        private static void Encapsulate(ref Bounds bounds, ref bool has, Bounds other)
        {
            if (!has)
            {
                bounds = other;
                has = true;
            }
            else
            {
                bounds.Encapsulate(other);
            }
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.normalized;
        }

        private static float Along(Vector3 point, Vector3 origin, Vector3 dir) => Vector3.Dot(point - origin, dir);

        // 경계 상자의 8개 꼭짓점을 dir에 투영해 가장 큰(또는 작은) 값을 구한다.
        private static float AlongExtent(Bounds b, Vector3 origin, Vector3 dir, bool max)
        {
            float best = max ? float.MinValue : float.MaxValue;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? b.min.x : b.max.x,
                    (i & 2) == 0 ? b.min.y : b.max.y,
                    (i & 4) == 0 ? b.min.z : b.max.z);
                float s = Along(corner, origin, dir);
                best = max ? Mathf.Max(best, s) : Mathf.Min(best, s);
            }

            return best;
        }

        private static float Extent(Bounds b, Vector3 origin, Vector3 dir, bool min) => AlongExtent(b, origin, dir, max: !min);

        private static string Fmt(Vector3 v) => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
    }
}
