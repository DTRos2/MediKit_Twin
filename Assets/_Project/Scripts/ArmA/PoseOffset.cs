using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 어떤 기준 자세(부모)에서 본 다른 자세(자식)의 상대 자세. 한 번 재 두고, 기준이 움직이면 자식이 같은 상대 자세로 따라가게 할 때 쓴다.
    /// 예를 들어 그리퍼가 물건을 쥔 순간의 도구 끝점과 물건 사이 상대 자세를 재 두면, 그 뒤 도구 끝점이 움직여도 물건이 손에 쥔 모양 그대로 따라간다.
    /// 씬 없이 시험한다.
    /// </summary>
    public readonly struct PoseOffset
    {
        private readonly Vector3 _localPosition;
        private readonly Quaternion _localRotation;

        private PoseOffset(Vector3 localPosition, Quaternion localRotation)
        {
            _localPosition = localPosition;
            _localRotation = localRotation;
        }

        /// <summary>parent에서 본 child의 상대 자세.</summary>
        public static PoseOffset Between(Pose parent, Pose child)
        {
            Quaternion inverse = Quaternion.Inverse(parent.rotation);
            return new PoseOffset(inverse * (child.position - parent.position), inverse * child.rotation);
        }

        /// <summary>parent가 이 자세에 있을 때 같은 상대 자세에 있는 자식의 월드 자세.</summary>
        public Pose ApplyTo(Pose parent)
        {
            return new Pose(parent.position + parent.rotation * _localPosition, parent.rotation * _localRotation);
        }
    }
}
