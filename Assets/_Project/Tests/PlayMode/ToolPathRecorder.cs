using System.Text;
using MediKitTwin.ArmA;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 도구 끝점이 지나간 길을 일정한 시간 간격으로 적는다. 팔이 목표로 곧게 가는지, 가는 도중 낮게 처지는지를 보려는 시험용 도구이며 읽기만 한다.
    /// </summary>
    public sealed class ToolPathRecorder
    {
        private readonly IkArmTarget _arm;
        private readonly float _intervalSeconds;
        private readonly StringBuilder _lines = new StringBuilder();
        private float _nextAt;

        public ToolPathRecorder(IkArmTarget arm, float intervalSeconds)
        {
            _arm = arm;
            _intervalSeconds = intervalSeconds;
        }

        public string Lines => _lines.ToString();

        public void Sample(float simSeconds)
        {
            if (_arm == null || simSeconds < _nextAt)
            {
                return;
            }

            _nextAt = simSeconds + _intervalSeconds;
            Pose tool = _arm.Model.ToolPose;
            _lines.AppendLine($"  [{simSeconds:0.00}s] 도구 끝점 ({tool.position.x:0.000},{tool.position.y:0.000},{tool.position.z:0.000}) 앞쪽 y={(tool.rotation * Vector3.forward).y:0.00}");
        }
    }
}
