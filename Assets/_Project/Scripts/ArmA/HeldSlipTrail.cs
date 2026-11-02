using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 쥔 물건이 손에서 미끄러지는 때를 가르려고, 지점마다 쥔 자세를 받아 직전 지점·처음 지점과 견준다.
    /// 처음과 지금만 견주면 "어딘가에서 미끄러졌다"까지만 알고, 지점별로 견주면 어느 구간(예: 박스까지 가는 길, 슬롯 위로 내리는 길)에서
    /// 미끄러졌는지 안다. 물리 엔진이나 씬을 모르고 쥔 자세(HeldItemFrame)만 다루므로 시험하기 쉽다.
    /// 놓을 물건 하나마다 새로 만든다.
    /// </summary>
    public sealed class HeldSlipTrail
    {
        private readonly HeldItemFrame _first;
        private HeldItemFrame _previous;

        /// <param name="first">비교의 기준이 되는 처음 쥔 자세.</param>
        public HeldSlipTrail(HeldItemFrame first)
        {
            _first = first;
            _previous = first;
        }

        /// <summary>지금 쥔 자세를 직전 지점, 처음 지점과 견준다. 지금 자세는 다음 지점의 "직전"이 된다.</summary>
        public SlipMark Mark(HeldItemFrame now)
        {
            var mark = new SlipMark(
                now.CenterInTool - _previous.CenterInTool,
                Quaternion.Angle(_previous.RotationInTool, now.RotationInTool),
                now.CenterInTool - _first.CenterInTool,
                Quaternion.Angle(_first.RotationInTool, now.RotationInTool));

            _previous = now;
            return mark;
        }
    }
}
