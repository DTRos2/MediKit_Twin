using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 그리퍼 프로필에 적힌 손가락 관절을 로봇 안에서 찾아 모아 두고, 관절·열린 값·닫힌 값·충돌체를 한곳에서 내준다.
    /// 그리퍼를 구동하는 쪽(JointGripper)과 지켜보는 쪽(시험 기록기)이 관절을 같은 방식으로 찾도록 한 곳에 둔 것이다.
    /// 찾지 못한 관절이 하나라도 있으면 만들지 않는다. 반쪽짜리 손가락 목록으로 구동하면 한쪽만 닫혀 물건을 놓치기 때문이다.
    /// </summary>
    public sealed class FingerJointSet
    {
        private readonly ArticulationBody[] _joints;
        private readonly float[] _openValues;
        private readonly float[] _closedValues;
        private readonly Collider[] _colliders;

        private FingerJointSet(ArticulationBody[] joints, float[] openValues, float[] closedValues, Collider[] colliders)
        {
            _joints = joints;
            _openValues = openValues;
            _closedValues = closedValues;
            _colliders = colliders;
        }

        public int Count => _joints.Length;

        public IReadOnlyList<ArticulationBody> Joints => _joints;

        /// <summary>관절마다 열렸을 때의 값. 직선 관절은 m, 회전 관절은 rad.</summary>
        public IReadOnlyList<float> OpenValues => _openValues;

        /// <summary>관절마다 닫혔을 때의 값. 단위는 열린 값과 같다.</summary>
        public IReadOnlyList<float> ClosedValues => _closedValues;

        /// <summary>손가락 관절과 그 아래 링크에 달린 충돌체 전부.</summary>
        public IReadOnlyList<Collider> Colliders => _colliders;

        /// <summary>호스트의 프로필에 적힌 손가락 관절을 이름으로 찾는다.</summary>
        /// <param name="error">실패하면 어떤 링크를 찾지 못했는지 적은 글. 성공하면 비어 있다.</param>
        public static bool TryCreate(IGripperHost host, out FingerJointSet set, out string error)
        {
            set = null;
            error = string.Empty;

            IReadOnlyList<ArmProfileSO.GripperJointEntry> entries = host.GripperJoints;
            var joints = new ArticulationBody[entries.Count];
            var openValues = new float[entries.Count];
            var closedValues = new float[entries.Count];
            var colliders = new List<Collider>();

            for (int i = 0; i < entries.Count; i++)
            {
                Transform link = TransformSearch.FindDeep(host.RobotRoot, entries[i].LinkName);
                ArticulationBody joint = link != null ? link.GetComponent<ArticulationBody>() : null;
                if (joint == null)
                {
                    error = $"그리퍼 관절 링크 '{entries[i].LinkName}'를 찾지 못했다.";
                    return false;
                }

                joints[i] = joint;
                openValues[i] = entries[i].OpenValue;
                closedValues[i] = entries[i].ClosedValue;
                colliders.AddRange(joint.GetComponentsInChildren<Collider>());
            }

            set = new FingerJointSet(joints, openValues, closedValues, colliders.ToArray());
            return true;
        }

        /// <summary>관절의 지금 위치를 프로필 값과 같은 단위(직선은 m, 회전은 rad)로 positions에 읽는다.</summary>
        public void ReadPositions(float[] positions)
        {
            for (int i = 0; i < _joints.Length; i++)
            {
                positions[i] = _joints[i].jointPosition[0];
            }
        }

        public bool IsRevolute(int index)
        {
            return _joints[index].jointType == ArticulationJointType.RevoluteJoint;
        }
    }
}
