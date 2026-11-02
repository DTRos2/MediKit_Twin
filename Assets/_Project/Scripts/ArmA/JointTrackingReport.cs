using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 관절마다 목표 각도와 지금 각도를 견주어, 목표를 따라가지 못하는 관절을 글자로 설명한다. 팔이 시간 안에 목표에 닿지 못했을 때
    /// "어느 관절이 얼마나 못 따라갔는지"와 "그 관절이 한계에 붙어 있는지"를 보여 준다. 한계에 붙어 있으면 한계 문제이고,
    /// 한계에서 먼데도 못 따라가면 무언가에 걸린 것이다. 글자 만드는 일만 하므로 씬 없이 시험한다. 각도는 모두 같은 기준(URDF)이다.
    /// </summary>
    public static class JointTrackingReport
    {
        /// <param name="errorThresholdDeg">목표와 지금 각도의 차이가 이 값(도) 이상인 관절만 적는다.</param>
        /// <param name="nearLimitDeg">지금 각도가 한계에서 이 값(도) 안이면 한계에 붙었다고 적는다.</param>
        public static string Describe(
            IReadOnlyList<string> names,
            IReadOnlyList<float> targetRad,
            IReadOnlyList<float> currentRad,
            IReadOnlyList<float> lowerRad,
            IReadOnlyList<float> upperRad,
            float errorThresholdDeg,
            float nearLimitDeg)
        {
            int count = names.Count;
            if (targetRad.Count != count || currentRad.Count != count || lowerRad.Count != count || upperRad.Count != count)
            {
                throw new ArgumentException("이름, 목표, 현재, 아래 한계, 위 한계의 개수가 같아야 한다.");
            }

            var lagging = new List<(float ErrorDeg, string Text)>();
            for (int i = 0; i < count; i++)
            {
                float targetDeg = targetRad[i] * Mathf.Rad2Deg;
                float currentDeg = currentRad[i] * Mathf.Rad2Deg;
                float errorDeg = Mathf.Abs(targetDeg - currentDeg);
                if (errorDeg < errorThresholdDeg)
                {
                    continue;
                }

                float lowerDeg = lowerRad[i] * Mathf.Rad2Deg;
                float upperDeg = upperRad[i] * Mathf.Rad2Deg;
                string text = $"{names[i]} 목표 {Number(targetDeg)}° 현재 {Number(currentDeg)}° (오차 {Number(errorDeg)}°, " +
                              $"한계 {Number(lowerDeg, "0")}~{Number(upperDeg, "0")}°{LimitNote(currentDeg, lowerDeg, upperDeg, nearLimitDeg)})";
                lagging.Add((errorDeg, text));
            }

            if (lagging.Count == 0)
            {
                return $"모든 관절의 목표와 현재 각도 차이가 {Number(errorThresholdDeg, "0.#")}° 이내";
            }

            return string.Join("; ", lagging.OrderByDescending(item => item.ErrorDeg).Select(item => item.Text));
        }

        /// <summary>목표와 지금 각도가 errorThresholdDeg(도) 이상 다른 관절이 하나라도 있으면 true. 개수가 다르면 비교할 수 없어 false.</summary>
        public static bool HasLagging(IReadOnlyList<float> targetRad, IReadOnlyList<float> currentRad, float errorThresholdDeg)
        {
            if (targetRad.Count != currentRad.Count)
            {
                return false;
            }

            for (int i = 0; i < targetRad.Count; i++)
            {
                if (Mathf.Abs(targetRad[i] - currentRad[i]) * Mathf.Rad2Deg >= errorThresholdDeg)
                {
                    return true;
                }
            }

            return false;
        }

        private static string LimitNote(float currentDeg, float lowerDeg, float upperDeg, float nearLimitDeg)
        {
            if (currentDeg <= lowerDeg + nearLimitDeg)
            {
                return ", 하한에 붙음";
            }

            if (currentDeg >= upperDeg - nearLimitDeg)
            {
                return ", 상한에 붙음";
            }

            return string.Empty;
        }

        private static string Number(float value, string format = "0.0")
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
