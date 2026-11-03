using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔 베이스를 한 번 옮기는 동안 물리 스텝마다 잰 값을 모아 요약한다. 물리 엔진이나 씬을 모르고 값만 다루므로 시험하기 쉽다.
    /// 지점 몇 곳에서만 재면 "접는 동안인지 베이스가 움직이는 동안인지"까지만 알고, 스텝마다 재면 밀림이 베이스가 움직이는 중에 커지는지
    /// 멈춘 뒤에 커지는지, 그때 속도가 얼마였는지까지 안다.
    /// 밀림은 첫 번째로 쥔 자세를 잰 스텝을 기준으로 한다. 쥔 자세를 재지 못한 스텝은 직전 값을 그대로 잇는다.
    /// </summary>
    public sealed class BaseMotionTrace
    {
        /// <summary>정하지 않았을 때 쓰는 값: 이 속도(m/s)를 넘으면 베이스가 움직이는 것으로 본다.</summary>
        public const float DefaultStillSpeedMps = 0.02f;

        /// <summary>정하지 않았을 때 쓰는 값: 이 회전 속도(도/초)를 넘으면 베이스가 도는 것으로 본다.</summary>
        public const float DefaultStillYawRateDegPerSec = 2f;

        private readonly List<BaseMotionSample> _samples = new List<BaseMotionSample>();
        private readonly float _onsetSlipM;
        private readonly float _stillSpeedMps;
        private readonly float _stillYawRateDegPerSec;

        /// <param name="onsetSlipM">쥔 물건이 이만큼(m) 밀리면 "밀리기 시작했다"고 본다.</param>
        /// <param name="stillSpeedMps">이 속도(m/s)를 넘으면 베이스가 움직이는 것으로 본다.</param>
        /// <param name="stillYawRateDegPerSec">이 회전 속도(도/초)를 넘으면 베이스가 도는 것으로 본다.</param>
        public BaseMotionTrace(float onsetSlipM, float stillSpeedMps = DefaultStillSpeedMps, float stillYawRateDegPerSec = DefaultStillYawRateDegPerSec)
        {
            _onsetSlipM = onsetSlipM;
            _stillSpeedMps = stillSpeedMps;
            _stillYawRateDegPerSec = stillYawRateDegPerSec;
        }

        public int Count => _samples.Count;

        public void Add(BaseMotionSample sample)
        {
            _samples.Add(sample);
        }

        public BaseMotionSummary Summarize()
        {
            int count = _samples.Count;
            var speeds = new float[count];
            var yawRates = new float[count];
            var slips = new float[count];
            var moving = new bool[count];

            var summary = new BaseMotionSummary { SampleCount = count };
            HeldItemFrame? reference = null;
            Vector3 slipVector = Vector3.zero;
            float slipDeg = 0f;
            int lastMovingIndex = -1;
            int onsetIndex = -1;
            float distanceAtIndex = 0f;
            var distances = new float[count];

            for (int i = 0; i < count; i++)
            {
                BaseMotionSample sample = _samples[i];

                if (i > 0)
                {
                    BaseMotionSample previous = _samples[i - 1];
                    float dt = sample.TimeSeconds - previous.TimeSeconds;
                    float stepDistance = PlanarDistance(previous.BasePosition, sample.BasePosition);
                    float stepYaw = Mathf.Abs(Mathf.DeltaAngle(previous.BaseYawDeg, sample.BaseYawDeg));

                    // 시간이 흐르지 않은 스텝은 속도를 구할 수 없다. 움직인 양은 그대로 센다.
                    if (dt > 1e-6f)
                    {
                        speeds[i] = stepDistance / dt;
                        yawRates[i] = stepYaw / dt;
                    }

                    summary.DistanceM += stepDistance;
                    summary.YawTravelDeg += stepYaw;
                    distanceAtIndex = summary.DistanceM;
                    moving[i] = speeds[i] > _stillSpeedMps || yawRates[i] > _stillYawRateDegPerSec;
                }

                distances[i] = distanceAtIndex;

                if (sample.Held.HasValue)
                {
                    HeldItemFrame now = sample.Held.Value;
                    if (!reference.HasValue)
                    {
                        reference = now;
                    }

                    slipVector = now.CenterInTool - reference.Value.CenterInTool;
                    slipDeg = Quaternion.Angle(reference.Value.RotationInTool, now.RotationInTool);
                }

                slips[i] = slipVector.magnitude;

                if (moving[i])
                {
                    lastMovingIndex = i;
                    summary.SlipAtLastMotionM = slips[i];
                    summary.HasMotion = true;
                }

                if (onsetIndex < 0 && slips[i] >= _onsetSlipM)
                {
                    onsetIndex = i;
                    summary.OnsetStepsSinceMotion = lastMovingIndex < 0 ? -1 : i - lastMovingIndex;
                }

                summary.MaxSpeedMps = Mathf.Max(summary.MaxSpeedMps, speeds[i]);
                summary.MaxYawRateDegPerSec = Mathf.Max(summary.MaxYawRateDegPerSec, yawRates[i]);
                summary.MaxSlipM = Mathf.Max(summary.MaxSlipM, slips[i]);
            }

            if (count > 0)
            {
                summary.DurationSeconds = _samples[count - 1].TimeSeconds - _samples[0].TimeSeconds;
                summary.StepSeconds = count > 1 ? summary.DurationSeconds / (count - 1) : 0f;
                summary.SlipAtEndM = slips[count - 1];
                summary.SlipAtEndInTool = slipVector;
                summary.SlipAtEndDeg = slipDeg;
            }

            if (onsetIndex >= 0)
            {
                summary.HasOnset = true;
                summary.OnsetSeconds = _samples[onsetIndex].TimeSeconds - _samples[0].TimeSeconds;
                summary.OnsetSpeedMps = speeds[onsetIndex];
                summary.OnsetYawRateDegPerSec = yawRates[onsetIndex];
                summary.OnsetDistanceM = distances[onsetIndex];
                summary.OnsetWhileMoving = moving[onsetIndex];
            }
            else
            {
                summary.OnsetStepsSinceMotion = -1;
            }

            summary.SpeedsMps = speeds;
            summary.YawRatesDegPerSec = yawRates;
            summary.SlipsM = slips;
            return summary;
        }

        // 바닥 평면(x, z)에서의 거리. 높이는 베이스가 바꿀 수 없다.
        private static float PlanarDistance(Vector3 from, Vector3 to)
        {
            return new Vector2(to.x - from.x, to.z - from.z).magnitude;
        }
    }
}
