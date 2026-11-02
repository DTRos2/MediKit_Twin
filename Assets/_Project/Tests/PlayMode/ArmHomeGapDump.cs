using System.Text;
using MediKitTwin.ArmA;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 관절마다 지금 각도가 홈(접은) 자세에서 얼마나 떨어져 있는지 적는다. 베이스를 옮기기 전에 팔을 접는 단계는
    /// 관절 하나라도 허용 오차를 넘으면 실패하는데, 관절 상태 출력의 "목표"는 구동 명령이라 명령이 지금 각도에서 최대 5°까지만 앞서 나가
    /// 막힌 관절의 남은 거리를 실제보다 작게 보여 준다. 그래서 명령이 아니라 홈 자세를 기준으로 다시 잰다.
    /// 읽기만 하며, 접기가 끝난 직후의 자세에서만 의미가 있다(접기가 아닌 동작 중에는 홈이 목표가 아니다).
    /// </summary>
    public sealed class ArmHomeGapDump
    {
        private readonly ArmController _arm;
        private readonly float _toleranceRad;

        public ArmHomeGapDump(ArmController arm, float toleranceRad)
        {
            _arm = arm;
            _toleranceRad = toleranceRad;
        }

        public string Describe()
        {
            if (_arm == null || !_arm.IsReady)
            {
                return "  팔 제어기(ArmController)를 찾지 못했거나 아직 준비되지 않았다\n";
            }

            int count = _arm.JointCount;
            var current = new float[count];
            _arm.GetJointPositions(current);
            float[] home = _arm.HomePoseRad;

            var text = new StringBuilder();
            text.AppendLine(
                $"  팔 접기 허용 오차 {_toleranceRad * Mathf.Rad2Deg:0.0}° — 하나라도 넘으면 접기 실패. " +
                $"팔 제어기가 보는 도착 여부(IsAtTarget): {_arm.IsAtTarget(_toleranceRad)}");

            for (int i = 0; i < count; i++)
            {
                float gapRad = current[i] - home[i];
                string mark = Mathf.Abs(gapRad) > _toleranceRad ? "  ← 허용 오차 초과" : string.Empty;
                text.AppendLine(
                    $"  {_arm.JointNames[i]}: 지금 {current[i] * Mathf.Rad2Deg:0.0}° 홈 {home[i] * Mathf.Rad2Deg:0.0}° " +
                    $"차이 {gapRad * Mathf.Rad2Deg:+0.0;-0.0}°{mark}");
            }

            return text.ToString();
        }
    }
}
