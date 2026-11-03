#if UNITY_EDITOR
using MediKitTwin.ArmA;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 쥐는 순간의 도구 좌표계가 월드에서 어느 쪽을 향하는지, 그리고 물건의 기준점(오브젝트 위치)과 충돌체 중심이 도구 좌표에서 어디인지 글로 적는다.
    /// 도구 좌표의 x·y·z가 월드의 위·아래·좌우 중 무엇인지 알아야 "물건이 패드 위로 올라왔다"는 말을 수치로 읽을 수 있고,
    /// 인식기가 알려 주는 위치는 물건의 기준점인데 손가락이 쥐는 곳은 충돌체 중심이라 둘이 어긋나는지도 여기서 가린다.
    /// </summary>
    public static class GraspFrameDescriber
    {
        private const float MetersToCm = 100f;

        /// <summary>"도구 축(월드) x (0.00,-1.00,0.00) y (...) z (...)"처럼 적는다. 월드의 위는 (0,1,0)이다.</summary>
        public static string Axes(Pose tool)
        {
            Vector3 x = tool.rotation * Vector3.right;
            Vector3 y = tool.rotation * Vector3.up;
            Vector3 z = tool.rotation * Vector3.forward;
            return $"도구 축(월드) x {Format(x)} · y {Format(y)} · z {Format(z)}";
        }

        /// <summary>물건 기준점과 충돌체 중심을 도구 끝점 기준 좌표(cm)로 적는다. 둘의 차이가 크면 기준점을 겨냥한 쥐기는 물건 몸체를 한쪽으로 치우치게 쥔다.</summary>
        public static string ItemAnchor(Pose tool, Graspable item)
        {
            Vector3 pivot = ToTool(tool, item.transform.position);

            // 충돌체 중심은 도구 축에 나란한 상자의 중심으로 잰다. Graspable.Center는 월드 축 기준이라 물건이 도구와 함께 돌면 어긋난다.
            var extent = new ToolFrameExtent(tool);
            foreach (Collider collider in item.Colliders)
            {
                extent.Include(ColliderPoints.Of(collider));
            }

            Vector3 center = extent.Center;
            Vector3 gap = center - pivot;
            return $"물건 기준점(도구 기준) {FormatCm(pivot)} · 충돌체 중심 {FormatCm(center)} · 중심-기준점 차이 {FormatCm(gap)}cm";
        }

        private static Vector3 ToTool(Pose tool, Vector3 worldPoint)
        {
            return Quaternion.Inverse(tool.rotation) * (worldPoint - tool.position);
        }

        private static string Format(Vector3 v)
        {
            return $"({v.x:+0.00;-0.00;0.00},{v.y:+0.00;-0.00;0.00},{v.z:+0.00;-0.00;0.00})";
        }

        private static string FormatCm(Vector3 v)
        {
            Vector3 cm = v * MetersToCm;
            return $"({cm.x:+0.0;-0.0;0.0},{cm.y:+0.0;-0.0;0.0},{cm.z:+0.0;-0.0;0.0})";
        }
    }
}
#endif
