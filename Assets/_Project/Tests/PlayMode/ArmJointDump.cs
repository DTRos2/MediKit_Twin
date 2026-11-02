using System.Text;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 팔 관절의 지금 각도, 구동 목표, 한계를 한 줄씩 적는다. 어느 관절이 목표를 못 따라갔는지 보려는 시험용 도구이며 읽기만 한다.
    /// </summary>
    public sealed class ArmJointDump
    {
        private readonly ArticulationBody[] _bodies;

        public ArmJointDump(ArticulationBody[] bodies)
        {
            _bodies = bodies;
        }

        public string Describe()
        {
            var text = new StringBuilder();
            foreach (ArticulationBody body in _bodies)
            {
                if (body == null || body.isRoot || body.jointType != ArticulationJointType.RevoluteJoint || body.dofCount == 0)
                {
                    continue;
                }

                ArticulationDrive drive = body.xDrive;
                float current = Mathf.Rad2Deg * body.jointPosition[0];
                text.AppendLine(
                    $"  {body.name}: 현재 {current:0.0}° 목표 {drive.target:0.0}° 오차 {drive.target - current:+0.0;-0.0}° " +
                    $"한계 {drive.lowerLimit:0}~{drive.upperLimit:0}° 속도 {Mathf.Rad2Deg * body.jointVelocity[0]:0.0}°/s " +
                    $"강성 {drive.stiffness:0} 감쇠 {drive.damping:0} 힘한계 {drive.forceLimit:0}");
            }

            return text.ToString();
        }
    }
}
