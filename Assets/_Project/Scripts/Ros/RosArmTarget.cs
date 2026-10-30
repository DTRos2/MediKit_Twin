using System.Collections.Generic;
using MediKitTwin.Common;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;

namespace MediKitTwin.Ros
{
    /// <summary>
    /// ROS가 /arm_a/joint_command로 보낸 최종 목표 각도를 IArmTarget으로 넘긴다. (FR-17)
    /// 켜져 있는 동안은 ROS 제어 모드이므로 배속을 1배로 고정한다. 목표까지의 이동은 ArmController가 처리한다.
    /// </summary>
    public class RosArmTarget : MonoBehaviour, IArmTarget
    {
        [SerializeField] private string _topic = "/arm_a/joint_command";
        [Tooltip("URDF 관절 이름. 순서가 TryGetTargets 배열 순서가 된다.")]
        [SerializeField] private string[] _jointNames = { "j1", "j2", "j3", "j4", "j5", "j6" };

        private float[] _latest;
        private bool _hasTarget;
        private ROSConnection _ros;

        public IReadOnlyList<string> JointNames => _jointNames;

        private void Awake()
        {
            _latest = new float[_jointNames.Length];
        }

        private void OnEnable()
        {
            SimClock.SetSpeedLocked(true);
            _ros = ROSConnection.GetOrCreateInstance();
            _ros.Subscribe<JointStateMsg>(_topic, OnJointCommand);
        }

        private void OnDisable()
        {
            _ros?.Unsubscribe(_topic);
            SimClock.SetSpeedLocked(false);
        }

        public bool TryGetTargets(float[] anglesRad)
        {
            if (!_hasTarget)
            {
                return false;
            }

            System.Array.Copy(_latest, anglesRad, _latest.Length);
            return true;
        }

        // 순서가 아니라 name으로 매칭한다. 관절이 하나라도 빠진 메시지는 일부만 반영되지 않도록 통째로 버린다.
        private void OnJointCommand(JointStateMsg msg)
        {
            for (int i = 0; i < _jointNames.Length; i++)
            {
                if (System.Array.IndexOf(msg.name, _jointNames[i]) < 0)
                {
                    return;
                }
            }

            for (int i = 0; i < _jointNames.Length; i++)
            {
                int index = System.Array.IndexOf(msg.name, _jointNames[i]);
                if (index >= msg.position.Length)
                {
                    return;
                }

                _latest[i] = (float)msg.position[index];
            }

            _hasTarget = true;
        }
    }
}
