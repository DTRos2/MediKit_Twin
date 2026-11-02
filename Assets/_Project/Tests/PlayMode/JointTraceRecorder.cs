using System.Collections.Generic;
using System.Text;
using MediKitTwin.ArmA;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 관절 하나의 각도와 속도, 팔 베이스 위치를 적되, 마지막으로 적은 값에서 눈에 띄게 변했을 때와 일정 시간이 지났을 때만 적는다.
    /// 실행 전체를 훑어도 로그가 길어지지 않으면서 "언제 그 각도로 갔는지, 베이스가 언제 움직였는지"가 남게 하려는 것이다.
    /// 가장 최근 몇 줄만 남기므로 오래된 줄은 버려진다. 시험용 도구이며 읽기만 한다.
    /// </summary>
    public sealed class JointTraceRecorder
    {
        private const float MinAngleChangeDeg = 1f;
        private const float MinBaseMoveM = 0.01f;
        private const float HeartbeatSeconds = 5f;

        private readonly ArmController _arm;
        private readonly int _jointIndex;
        private readonly float _intervalSeconds;
        private readonly int _keepLines;
        private readonly Queue<string> _lines = new Queue<string>();
        private float[] _angles;
        private float[] _velocities;
        private float _nextAt;
        private float _loggedAngleDeg = float.NaN;
        private Vector3 _loggedBase;
        private float _loggedAt = float.NegativeInfinity;

        public JointTraceRecorder(ArmController arm, int jointIndex, float intervalSeconds, int keepLines)
        {
            _arm = arm;
            _jointIndex = jointIndex;
            _intervalSeconds = intervalSeconds;
            _keepLines = keepLines;
        }

        public string Lines
        {
            get
            {
                var text = new StringBuilder();
                foreach (string line in _lines)
                {
                    text.AppendLine(line);
                }

                return text.ToString();
            }
        }

        public void Sample(float simSeconds)
        {
            if (_arm == null || !_arm.IsReady || _jointIndex >= _arm.JointCount || simSeconds < _nextAt)
            {
                return;
            }

            _nextAt = simSeconds + _intervalSeconds;
            if (_angles == null)
            {
                _angles = new float[_arm.JointCount];
                _velocities = new float[_arm.JointCount];
            }

            _arm.GetJointPositions(_angles);
            _arm.GetJointVelocities(_velocities);
            float angleDeg = _angles[_jointIndex] * Mathf.Rad2Deg;
            Vector3 basePosition = _arm.BaseWorldPosition;

            if (!HasChanged(simSeconds, angleDeg, basePosition))
            {
                return;
            }

            _loggedAngleDeg = angleDeg;
            _loggedBase = basePosition;
            _loggedAt = simSeconds;
            _lines.Enqueue(
                $"  [{simSeconds:0.00}s] {_arm.JointNames[_jointIndex]} {angleDeg:0.0}° 속도 {_velocities[_jointIndex] * Mathf.Rad2Deg:0.0}°/s, " +
                $"팔 베이스 ({basePosition.x:0.000},{basePosition.y:0.000},{basePosition.z:0.000})");

            while (_lines.Count > _keepLines)
            {
                _lines.Dequeue();
            }
        }

        private bool HasChanged(float simSeconds, float angleDeg, Vector3 basePosition)
        {
            return float.IsNaN(_loggedAngleDeg)
                   || Mathf.Abs(angleDeg - _loggedAngleDeg) >= MinAngleChangeDeg
                   || (basePosition - _loggedBase).magnitude >= MinBaseMoveM
                   || simSeconds - _loggedAt >= HeartbeatSeconds;
        }
    }
}
