using System.Text;
using MediKitTwin.ArmA;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 지금 도구 끝점의 월드 자세와, 지정한 선반 칸 안 물건들의 위치·크기를 적는다. 팔이 물건 높이에 맞게 갔는지 숫자로 보려는 시험용 도구이며 읽기만 한다.
    /// </summary>
    public sealed class ArmPoseSnapshot
    {
        private readonly IkArmTarget _arm;
        private readonly string _cellName;

        public ArmPoseSnapshot(IkArmTarget arm, string cellName)
        {
            _arm = arm;
            _cellName = cellName;
        }

        public string Describe()
        {
            var text = new StringBuilder();
            if (_arm != null)
            {
                Pose tool = _arm.Model.ToolPose;
                text.AppendLine($"  도구 끝점: 위치 ({tool.position.x:0.000},{tool.position.y:0.000},{tool.position.z:0.000}) 앞쪽 {tool.rotation * Vector3.forward}");
            }

            GameObject cell = GameObject.Find(_cellName);
            if (cell == null)
            {
                text.AppendLine($"  {_cellName}을 찾지 못했다");
                return text.ToString();
            }

            foreach (Renderer renderer in cell.GetComponentsInChildren<Renderer>())
            {
                Bounds b = renderer.bounds;
                text.AppendLine($"  {renderer.name}: 중심 ({b.center.x:0.000},{b.center.y:0.000},{b.center.z:0.000}) 크기 ({b.size.x:0.000},{b.size.y:0.000},{b.size.z:0.000}) 아래 y={b.min.y:0.000} 위 y={b.max.y:0.000}");
            }

            return text.ToString();
        }
    }
}
