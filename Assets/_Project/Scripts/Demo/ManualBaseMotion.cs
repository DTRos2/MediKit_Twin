using System;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 손으로 베이스를 움직일 때 한 프레임에 얼마나 옮기고 돌릴지 계산한다. 앞뒤와 좌우는 베이스가 보는 방향 기준이고,
    /// 대각선으로 눌러도 한 방향으로 눌렀을 때보다 빠르지 않다. 서는 자리를 눈으로 맞춰 보는 도구이므로 옆으로도 움직일 수 있다.
    /// 입력을 읽는 코드와 분리해 씬 없이 시험한다.
    /// </summary>
    public sealed class ManualBaseMotion
    {
        private readonly float _moveSpeedMps;
        private readonly float _turnSpeedDegPerSec;
        private readonly float _fastMultiplier;

        /// <param name="moveSpeedMps">이동 속도(m/s).</param>
        /// <param name="turnSpeedDegPerSec">제자리 회전 속도(도/초).</param>
        /// <param name="fastMultiplier">빠르게 움직일 때 속도에 곱하는 값. 1 이상이다.</param>
        public ManualBaseMotion(float moveSpeedMps, float turnSpeedDegPerSec, float fastMultiplier)
        {
            if (moveSpeedMps <= 0f || turnSpeedDegPerSec <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(moveSpeedMps), "이동 속도와 회전 속도는 0보다 커야 한다.");
            }

            if (fastMultiplier < 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(fastMultiplier), "빠르게 곱하는 값은 1 이상이어야 한다.");
            }

            _moveSpeedMps = moveSpeedMps;
            _turnSpeedDegPerSec = turnSpeedDegPerSec;
            _fastMultiplier = fastMultiplier;
        }

        /// <summary>입력을 deltaSeconds 동안 누른 뒤의 자리. 높이는 바꾸지 않고, 방향은 0~360도로 맞춘다.</summary>
        public BasePlacement Advance(BasePlacement current, BaseMoveInput input, float deltaSeconds)
        {
            float scale = input.IsFast ? _fastMultiplier : 1f;

            Vector3 local = Vector3.ClampMagnitude(new Vector3(input.Right, 0f, input.Forward), 1f);
            Vector3 heading = Quaternion.Euler(0f, current.YawDeg, 0f) * local;
            Vector3 position = current.Position + heading * (_moveSpeedMps * scale * deltaSeconds);

            float yawDeg = Mathf.Repeat(current.YawDeg + input.Turn * _turnSpeedDegPerSec * scale * deltaSeconds, 360f);

            return new BasePlacement(position, yawDeg);
        }
    }
}
