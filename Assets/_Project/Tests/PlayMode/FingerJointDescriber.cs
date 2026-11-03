using MediKitTwin.ArmA;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 손가락 관절의 값을 사람이 읽는 단위로 바꿔 글로 만든다. 읽기만 한다.
    /// 직선 관절은 m를 mm로, 회전 관절은 rad를 도로 바꿔서 종류가 달라도 숫자를 같은 눈으로 읽게 한다.
    /// </summary>
    public sealed class FingerJointDescriber
    {
        private const float MetersToMm = 1000f;

        private readonly FingerJointSet _fingers;

        public FingerJointDescriber(FingerJointSet fingers)
        {
            _fingers = fingers;
        }

        public int Count => _fingers.Count;

        /// <summary>관절 위치를 읽어 직선은 mm, 회전은 도로 바꾼 값들.</summary>
        public float[] ReadDisplayPositions()
        {
            var shown = new float[_fingers.Count];
            for (int i = 0; i < shown.Length; i++)
            {
                shown[i] = _fingers.Joints[i].jointPosition[0] * DisplayScale(i);
            }

            return shown;
        }

        /// <summary>관절 하나의 목표·현재 위치·닫힘 비율·구동 설정·걸린 힘을 한 줄로 적는다.</summary>
        public string Describe(int index)
        {
            ArticulationBody joint = _fingers.Joints[index];
            float span = _fingers.ClosedValues[index] - _fingers.OpenValues[index];
            float ratio = Mathf.Abs(span) > 1e-6f ? (joint.jointPosition[0] - _fingers.OpenValues[index]) / span : float.NaN;

            // 구동 목표는 직선 관절이면 m, 회전 관절이면 도다. 현재 위치는 직선 m, 회전 rad이라 같은 단위로 맞춰 적는다.
            bool revolute = _fingers.IsRevolute(index);
            string unit = revolute ? "°" : "mm";
            float targetShown = joint.xDrive.target * (revolute ? 1f : MetersToMm);
            float scale = DisplayScale(index);

            return $"관절 {joint.name}: 목표 {targetShown:0.0}{unit}, 현재 {joint.jointPosition[0] * scale:0.0}{unit}, 닫힘 비율 {ratio:0.00} · " +
                   $"구동 강성 {joint.xDrive.stiffness:0} 감쇠 {joint.xDrive.damping:0} 힘한계 {joint.xDrive.forceLimit:0} 종류 {joint.xDrive.driveType}, " +
                   $"관절에 걸린 힘 {joint.jointForce[0]:0.0}, 속도 {joint.jointVelocity[0] * scale:0.0}{unit}/s, 질량 {joint.mass:0.000}kg, " +
                   $"한계 {joint.xDrive.lowerLimit * MetersToMm:0.0}~{joint.xDrive.upperLimit * MetersToMm:0.0}";
        }

        private float DisplayScale(int index)
        {
            return _fingers.IsRevolute(index) ? Mathf.Rad2Deg : MetersToMm;
        }
    }
}
