using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔 베이스를 한 번 옮기는 동안 잰 것의 요약: 베이스가 얼마나 어떻게 움직였고, 쥔 물건이 언제 얼마나 손에서 밀렸는지.
    /// 값은 BaseMotionTrace가 채우고 읽기만 한다. 밀림은 이동을 시작한 순간의 쥔 자세와 견준 값이다.
    /// </summary>
    public sealed class BaseMotionSummary
    {
        /// <summary>잰 스텝의 수. 둘 미만이면 시간이 흐르지 않아 속도를 구할 수 없다.</summary>
        public int SampleCount { get; internal set; }

        public float DurationSeconds { get; internal set; }

        /// <summary>스텝 사이의 평균 간격(초). 스텝이 하나뿐이면 0이다.</summary>
        public float StepSeconds { get; internal set; }

        /// <summary>베이스가 바닥 평면에서 움직인 길이의 합(m).</summary>
        public float DistanceM { get; internal set; }

        /// <summary>베이스가 제자리에서 돈 각도의 합(도).</summary>
        public float YawTravelDeg { get; internal set; }

        public float MaxSpeedMps { get; internal set; }

        public float MaxYawRateDegPerSec { get; internal set; }

        /// <summary>베이스가 움직인 스텝이 하나라도 있었는지(BaseMotionTrace의 StillSpeedMps, StillYawRateDegPerSec 기준).</summary>
        public bool HasMotion { get; internal set; }

        /// <summary>이동 중 쥔 물건 중심이 도구 기준으로 가장 많이 밀린 거리(m).</summary>
        public float MaxSlipM { get; internal set; }

        /// <summary>마지막 스텝에서 밀린 거리(m).</summary>
        public float SlipAtEndM { get; internal set; }

        /// <summary>마지막 스텝에서 밀린 방향과 크기, 도구 기준(m).</summary>
        public Vector3 SlipAtEndInTool { get; internal set; }

        /// <summary>마지막 스텝에서 물건이 도구 기준으로 돈 각도(도).</summary>
        public float SlipAtEndDeg { get; internal set; }

        /// <summary>베이스가 마지막으로 움직인 스텝에서 밀린 거리(m). 움직이지 않았으면 0이다. 끝 값과 견주면 멈춘 뒤에 더 밀렸는지 안다.</summary>
        public float SlipAtLastMotionM { get; internal set; }

        /// <summary>밀림이 기준 거리를 처음 넘었는지.</summary>
        public bool HasOnset { get; internal set; }

        /// <summary>처음 넘은 때, 이동을 시작한 지 몇 초 뒤인지.</summary>
        public float OnsetSeconds { get; internal set; }

        public float OnsetSpeedMps { get; internal set; }

        public float OnsetYawRateDegPerSec { get; internal set; }

        /// <summary>처음 넘은 때까지 베이스가 움직인 길이(m).</summary>
        public float OnsetDistanceM { get; internal set; }

        /// <summary>처음 넘은 스텝에서 베이스가 움직이고 있었는지.</summary>
        public bool OnsetWhileMoving { get; internal set; }

        /// <summary>
        /// 처음 넘은 스텝이 베이스가 마지막으로 움직인 스텝에서 몇 스텝 뒤인지. 움직이는 중이면 0, 그때까지 한 번도 안 움직였으면 -1이다.
        /// </summary>
        public int OnsetStepsSinceMotion { get; internal set; }

        /// <summary>스텝마다 베이스의 평면 속도(m/s). 첫 스텝은 0이다.</summary>
        public IReadOnlyList<float> SpeedsMps { get; internal set; }

        /// <summary>스텝마다 베이스의 회전 속도(도/초). 첫 스텝은 0이다.</summary>
        public IReadOnlyList<float> YawRatesDegPerSec { get; internal set; }

        /// <summary>스텝마다 쥔 물건 중심이 밀린 거리(m).</summary>
        public IReadOnlyList<float> SlipsM { get; internal set; }
    }
}
