using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 슬라이더의 값과 화면 위치를 서로 바꾼다. 마우스 x에서 값을 구하고, 값에서 손잡이 위치를 구한다.
    /// 범위를 벗어난 입력은 끝으로 자르고, 범위가 없는(최소 ≥ 최대) 슬라이더는 최소값 자리에 둔다. Unity 없이 시험할 수 있다.
    /// </summary>
    public static class SliderMapping
    {
        /// <summary>값이 범위의 어디쯤인지(0~1).</summary>
        public static float ToFraction(float value, float min, float max)
        {
            if (max <= min)
            {
                return 0f;
            }

            return Mathf.Clamp01((value - min) / (max - min));
        }

        /// <summary>범위의 어디쯤(0~1)인지로 값을 구한다.</summary>
        public static float FromFraction(float fraction, float min, float max)
        {
            return Mathf.Lerp(min, max, Mathf.Clamp01(fraction));
        }

        /// <summary>마우스 x 좌표가 가리키는 값. trackStartX부터 trackWidth만큼이 움직이는 구간이다.</summary>
        public static float ValueAtX(float x, float trackStartX, float trackWidth, float min, float max)
        {
            if (trackWidth <= 0f)
            {
                return min;
            }

            return FromFraction((x - trackStartX) / trackWidth, min, max);
        }

        /// <summary>손잡이 가운데의 x 좌표.</summary>
        public static float XOfValue(float value, float trackStartX, float trackWidth, float min, float max)
        {
            return trackStartX + trackWidth * ToFraction(value, min, max);
        }
    }
}
