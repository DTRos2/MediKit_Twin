using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 알려 준 집는 자세(TaughtPickPose)로 물건을 집으러 갈 때의 자리와 도구 끝점 경로를 정한다.
    /// 팔 베이스는 칸에서 바깥으로 접근 여유만큼 물러선 경유지를 거쳐 알려 준 자리로 들어가고(칸 정면에서 수직으로 들어가 선반 모서리를 스치지 않게),
    /// 도구는 알려 준 도구 끝점 자세로 쥔다. 들어가기 전과 쥔 뒤 빼는 자리는 도구가 칸 안으로 들어가는 축(+Z)을 따라 앞뒤로 정한다.
    /// 알려 준 자세가 이미 닿는 방향이므로 방향을 찾는 일은 하지 않는다. 순수 계산이라 씬 없이 시험한다.
    /// </summary>
    public readonly struct TaughtPickPlan
    {
        private TaughtPickPlan(
            Vector3? armBaseWaypoint, Vector3 armBaseStand, float rootYawDeg,
            Quaternion toolRotation, Vector3 nearPosition, Vector3 graspPosition, Vector3 retreatPosition)
        {
            ArmBaseWaypoint = armBaseWaypoint;
            ArmBaseStand = armBaseStand;
            RootYawDeg = rootYawDeg;
            ToolRotation = toolRotation;
            NearPosition = nearPosition;
            GraspPosition = graspPosition;
            RetreatPosition = retreatPosition;
        }

        /// <summary>서는 자리로 들어가기 전에 거치는 팔 베이스 경유지. 접근 여유가 없으면 null.</summary>
        public Vector3? ArmBaseWaypoint { get; }

        /// <summary>팔 베이스가 설 자리.</summary>
        public Vector3 ArmBaseStand { get; }

        /// <summary>서는 자리에서 루트가 보는 방향(도).</summary>
        public float RootYawDeg { get; }

        /// <summary>집을 때의 도구 방향. 접근, 집기, 후퇴 모두 같은 방향을 쓴다.</summary>
        public Quaternion ToolRotation { get; }

        /// <summary>물건 앞쪽에서 들어가기 시작하는 도구 끝점 위치.</summary>
        public Vector3 NearPosition { get; }

        /// <summary>쥐는 도구 끝점 위치.</summary>
        public Vector3 GraspPosition { get; }

        /// <summary>쥔 뒤 칸 밖으로 빼는 도구 끝점 위치.</summary>
        public Vector3 RetreatPosition { get; }

        /// <summary>알려 준 쥐는 자리에서 물건 중심(월드)까지의 수평 방향 차이(높이는 0). 알려 준 뒤 물건 자리가 바뀌었으면 0이 아니다.</summary>
        public Vector3 HorizontalDeltaTo(Vector3 itemCenter)
        {
            return new Vector3(itemCenter.x - GraspPosition.x, 0f, itemCenter.z - GraspPosition.z);
        }

        /// <summary>
        /// 서는 자리와 도구 끝점 경로(접근, 집기, 후퇴)를 모두 delta만큼 옮긴 계획. 도구 방향과 서는 방향은 그대로다.
        /// 서는 자리를 옮기고 팔은 알려 준 관절 모양 그대로 두면 도구 끝점이 같은 만큼 옮겨지므로, 알려 준 자세를 다른 자리의 물건에 그대로 쓸 수 있다.
        /// 베이스는 바닥에서만 움직이므로 수평 이동(delta.y가 0)에 쓴다.
        /// </summary>
        public TaughtPickPlan Shifted(Vector3 delta)
        {
            return new TaughtPickPlan(
                ArmBaseWaypoint.HasValue ? ArmBaseWaypoint.Value + delta : (Vector3?)null,
                ArmBaseStand + delta,
                RootYawDeg,
                ToolRotation,
                NearPosition + delta,
                GraspPosition + delta,
                RetreatPosition + delta);
        }

        /// <param name="outward">칸에서 바깥으로 나가는 방향(수평 성분만 쓴다). 영벡터면 경유지 없이 서는 자리로 간다.</param>
        /// <param name="waypointExtraM">서는 자리에서 칸 바깥쪽으로 더 물러나 있는 경유지까지의 거리(m). 0 이하이면 경유지가 없다.</param>
        /// <param name="approachDistanceM">쥐기 전에 도구 축을 따라 물러나 있는 거리(m).</param>
        /// <param name="retreatDistanceM">쥔 뒤 도구 축을 따라 빼내는 거리(m).</param>
        public static TaughtPickPlan For(
            TaughtPickPose pose, Vector3 outward, float waypointExtraM, float approachDistanceM, float retreatDistanceM)
        {
            if (pose == null)
            {
                throw new ArgumentNullException(nameof(pose));
            }

            Vector3 stand = pose.ArmBasePosition;
            Vector3 flatOutward = new Vector3(outward.x, 0f, outward.z);
            Vector3? waypoint = null;
            if (waypointExtraM > 0f && flatOutward.sqrMagnitude > 1e-8f)
            {
                waypoint = stand + flatOutward.normalized * waypointExtraM;
            }

            Pose tool = pose.ToolPose;
            Vector3 insertDirection = tool.rotation * Vector3.forward;
            return new TaughtPickPlan(
                waypoint,
                stand,
                pose.RootYawDeg,
                tool.rotation,
                tool.position - insertDirection * approachDistanceM,
                tool.position,
                tool.position - insertDirection * retreatDistanceM);
        }
    }
}
