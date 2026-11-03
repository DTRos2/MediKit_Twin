using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물건 하나를 집기 위한 세 자리(접근, 쥐는 자리, 후퇴)와 그 자리에서의 도구 방향, 그리고 관절 각도 길(있으면).
    /// 카메라로 물건을 본 뒤 세 자리를 같은 만큼 옮겨 고칠 때 한 덩어리로 다룬다.
    /// </summary>
    public readonly struct GraspTargets
    {
        public GraspTargets(Quaternion rotation, Vector3 near, Vector3 grasp, Vector3 retreat, TaughtJointRoute route)
        {
            Rotation = rotation;
            Near = near;
            Grasp = grasp;
            Retreat = retreat;
            Route = route;
        }

        /// <summary>세 자리에서 같은 도구 방향(월드).</summary>
        public Quaternion Rotation { get; }

        public Vector3 Near { get; }

        public Vector3 Grasp { get; }

        public Vector3 Retreat { get; }

        /// <summary>관절 각도로 가는 길. 도구 끝점 자세로 가면 null.</summary>
        public TaughtJointRoute Route { get; }

        public Pose NearPose => new Pose(Near, Rotation);

        public Pose GraspPose => new Pose(Grasp, Rotation);

        public Pose RetreatPose => new Pose(Retreat, Rotation);

        /// <summary>세 자리를 offset만큼 옮긴다. 관절 길은 옮긴 자리에 맞춰 새로 푼 것을 받는다(없으면 null).</summary>
        public GraspTargets ShiftedBy(Vector3 offset, TaughtJointRoute route)
        {
            return new GraspTargets(Rotation, Near + offset, Grasp + offset, Retreat + offset, route);
        }

        /// <summary>
        /// 도구 끝점은 그대로 두고 세 자리의 도구 방향만 손가락이 닫히는 축(도구의 y축) 둘레로 tiltDeg만큼 돌린다. 양수이면 손가락 끝이 아래로 숙는다.
        /// 손가락 뿌리의 받침 블록은 패드보다 뒤에 있어서, 끝을 숙이면 뿌리는 올라가고 패드 끝만 내려가
        /// 선반 판 위에 놓인 얇은 물건 높이까지 패드가 닿는다. 관절 길은 돌린 방향에 맞춰 새로 푼 것을 받는다(없으면 null).
        /// </summary>
        public GraspTargets TiltedBy(float tiltDeg, TaughtJointRoute route)
        {
            Quaternion tilted = Rotation * Quaternion.AngleAxis(tiltDeg, Vector3.up);
            return new GraspTargets(tilted, Near, Grasp, Retreat, route);
        }
    }
}
