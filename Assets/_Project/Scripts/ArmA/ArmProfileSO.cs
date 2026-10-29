using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 로봇 한 종류(FR5, UR3 등)의 차이를 데이터로 모은 프로필. 팔 구동, IK, 그리퍼 코드는 로봇 종류를 모르고
    /// 이 프로필만 읽는다. 새 로봇은 프로필 파일 1개만 추가하면 된다.
    /// 링크 이름은 URDF의 link name이며 Unity에서는 GameObject 이름과 같다.
    /// </summary>
    [CreateAssetMenu(fileName = "ArmProfile", menuName = "MediKitTwin/Arm Profile")]
    public class ArmProfileSO : ScriptableObject
    {
        /// <summary>팔 관절 하나. JointName은 IArmTarget(ROS)이 쓰는 URDF 관절 이름이다.</summary>
        [Serializable]
        public class JointEntry
        {
            [SerializeField] private string _jointName;
            [SerializeField] private string _linkName;

            public JointEntry() { }

            public JointEntry(string jointName, string linkName)
            {
                _jointName = jointName;
                _linkName = linkName;
            }

            public string JointName => _jointName;

            /// <summary>이 관절이 움직이는 쪽(child) 링크 이름.</summary>
            public string LinkName => _linkName;
        }

        /// <summary>그리퍼 관절 하나. 개폐 정도(0~1)를 열림/닫힘 값 사이에서 보간해 구동한다.</summary>
        [Serializable]
        public class GripperJointEntry
        {
            [SerializeField] private string _linkName;
            [Tooltip("열렸을 때 관절 값. 직선 관절은 m, 회전 관절은 rad.")]
            [SerializeField] private float _openValue;
            [Tooltip("닫혔을 때 관절 값. mimic 관절은 multiplier를 곱한 값을 적는다.")]
            [SerializeField] private float _closedValue;

            public GripperJointEntry() { }

            public GripperJointEntry(string linkName, float openValue, float closedValue)
            {
                _linkName = linkName;
                _openValue = openValue;
                _closedValue = closedValue;
            }

            public string LinkName => _linkName;
            public float OpenValue => _openValue;
            public float ClosedValue => _closedValue;
        }

        [Header("팔")]
        [SerializeField] private string _baseLinkName = "base_link";
        [SerializeField] private JointEntry[] _joints = Array.Empty<JointEntry>();
        [SerializeField, Min(1f)] private float _maxJointSpeedDegPerSec = 90f;

        [Header("도구 끝점(TCP)")]
        [Tooltip("끝점이 붙는 링크. 보통 그리퍼 베이스 링크.")]
        [SerializeField] private string _toolLinkName;
        [Tooltip("도구 링크 좌표계 기준 끝점 위치(Unity 로컬, m). 두 손가락 사이 중앙.")]
        [SerializeField] private Vector3 _toolPositionOffset;
        [Tooltip("끝점의 +Z(forward)가 접근 방향이 되도록 맞추는 회전(오일러, 도).")]
        [SerializeField] private Vector3 _toolEulerOffset;

        [Header("그리퍼")]
        [SerializeField] private GripperJointEntry[] _gripperJoints = Array.Empty<GripperJointEntry>();

        [Header("역기구학")]
        [SerializeField] private DlsIkSettings _ik = new DlsIkSettings();

        public string BaseLinkName => _baseLinkName;
        public JointEntry[] Joints => _joints;
        public float MaxJointSpeedRadPerSec => _maxJointSpeedDegPerSec * Mathf.Deg2Rad;
        public string ToolLinkName => _toolLinkName;
        public Vector3 ToolPositionOffset => _toolPositionOffset;
        public Quaternion ToolRotationOffset => Quaternion.Euler(_toolEulerOffset);
        public GripperJointEntry[] GripperJoints => _gripperJoints;
        public DlsIkSettings Ik => _ik;

        /// <summary>에디터 도구(프로필 생성 메뉴)가 기본 프로필을 채울 때 쓴다.</summary>
        public void Configure(
            string baseLinkName,
            JointEntry[] joints,
            float maxJointSpeedDegPerSec,
            string toolLinkName,
            Vector3 toolPositionOffset,
            Vector3 toolEulerOffset,
            GripperJointEntry[] gripperJoints)
        {
            _baseLinkName = baseLinkName;
            _joints = joints;
            _maxJointSpeedDegPerSec = maxJointSpeedDegPerSec;
            _toolLinkName = toolLinkName;
            _toolPositionOffset = toolPositionOffset;
            _toolEulerOffset = toolEulerOffset;
            _gripperJoints = gripperJoints;
        }
    }
}
