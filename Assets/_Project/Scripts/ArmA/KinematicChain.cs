using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 회전 관절로만 이루어진 직렬 체인의 순기구학. 시뮬레이션 상태와 무관한 순수 계산이라
    /// IK가 물리 엔진 없이 어느 자세든 미리 풀 수 있고, 강화학습에서 목표 도달 가능성을 검사하는 데도 쓴다.
    /// 좌표는 모두 베이스 링크 기준이다.
    /// </summary>
    public sealed class KinematicChain
    {
        /// <summary>한 관절. 각도 0일 때 이전 프레임에서 이 관절 링크까지의 변환과 회전축.</summary>
        public readonly struct JointSpec
        {
            public readonly Vector3 LocalPosition;
            public readonly Quaternion LocalRotation;
            public readonly Vector3 Axis;
            public readonly Vector3 Pivot;
            public readonly float LowerRad;
            public readonly float UpperRad;

            public JointSpec(Vector3 localPosition, Quaternion localRotation, Vector3 axis, Vector3 pivot, float lowerRad, float upperRad)
            {
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                Axis = axis.normalized;
                Pivot = pivot;
                LowerRad = lowerRad;
                UpperRad = upperRad;
            }
        }

        private readonly JointSpec[] _joints;
        private readonly Vector3 _toolPosition;
        private readonly Quaternion _toolRotation;

        public KinematicChain(JointSpec[] joints, Vector3 toolPositionInLastLink, Quaternion toolRotationInLastLink)
        {
            _joints = joints;
            _toolPosition = toolPositionInLastLink;
            _toolRotation = toolRotationInLastLink;
        }

        public int JointCount => _joints.Length;

        public JointSpec GetJoint(int index) => _joints[index];

        /// <summary>관절 각도(rad)로 도구 끝점의 베이스 기준 자세를 구한다.</summary>
        public Pose Forward(float[] anglesRad)
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            for (int i = 0; i < _joints.Length; i++)
            {
                JointSpec joint = _joints[i];
                Quaternion before = rotation * joint.LocalRotation;
                Vector3 positionBefore = position + rotation * joint.LocalPosition;
                Quaternion spin = Quaternion.AngleAxis(anglesRad[i] * Mathf.Rad2Deg, joint.Axis);

                rotation = before * spin;
                // 관절 피벗이 링크 원점이 아닐 때 링크가 피벗을 중심으로 도는 효과를 반영한다.
                position = positionBefore + before * (joint.Pivot - spin * joint.Pivot);
            }

            return new Pose(position + rotation * _toolPosition, rotation * _toolRotation);
        }

        public void ClampToLimits(float[] anglesRad)
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                anglesRad[i] = Mathf.Clamp(anglesRad[i], _joints[i].LowerRad, _joints[i].UpperRad);
            }
        }
    }
}
