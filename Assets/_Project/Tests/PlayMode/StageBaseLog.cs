using System.Text;
using MediKitTwin.ArmA;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 단계(구성품의 집기·옮기기·놓기 등)가 바뀔 때마다 그 순간의 팔 베이스 위치, 첫째 관절 각도, 팔이 파고든 곳을 한 줄씩 적는다.
    /// 통과한 시험과 실패한 시험을 같은 시점에서 견주려는 시험용 도구이며 읽기만 한다.
    /// </summary>
    public sealed class StageBaseLog
    {
        private readonly ArmController _arm;
        private readonly ArmOverlapProbe _overlapProbe;
        private readonly StringBuilder _lines = new StringBuilder();

        public StageBaseLog(ArmController arm, ArmOverlapProbe overlapProbe)
        {
            _arm = arm;
            _overlapProbe = overlapProbe;
        }

        public string Lines => _lines.ToString();

        public void Record(float simSeconds, string stage)
        {
            if (_arm == null || !_arm.IsReady)
            {
                return;
            }

            var angles = new float[_arm.JointCount];
            _arm.GetJointPositions(angles);
            Vector3 basePosition = _arm.BaseWorldPosition;
            _lines.AppendLine(
                $"  [{simSeconds:0.0}s] {stage} — 팔 베이스 ({basePosition.x:0.000},{basePosition.y:0.000},{basePosition.z:0.000}), " +
                $"{_arm.JointNames[0]} {angles[0] * Mathf.Rad2Deg:0.0}°, 파고든 곳: {_overlapProbe.Summarize()}");
        }
    }
}
