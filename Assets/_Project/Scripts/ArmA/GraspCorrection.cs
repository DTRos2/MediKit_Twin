using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 예상한 물건 위치와 카메라로 본 위치의 차이로, 집는 자리를 얼마나 옮길지 정한다. 순수 계산이라 씬 없이 시험한다.
    /// 차이가 작으면 옮기지 않고(잡음 때문에 팔이 공연히 움직이지 않게), 너무 크면 한계까지만 옮긴다(잘못 인식해도 팔이 크게 움직이지 않게).
    /// </summary>
    public static class GraspCorrection
    {
        /// <param name="expected">예상한 물건 위치(월드).</param>
        /// <param name="observed">카메라로 본 물건 위치(월드).</param>
        /// <param name="minM">이보다 적게 다르면 옮기지 않는다(m).</param>
        /// <param name="maxM">이보다 크게 다르면 이 거리까지만 옮긴다(m).</param>
        /// <param name="isClamped">차이가 한계를 넘어 한계까지만 옮겼으면 true.</param>
        /// <returns>집는 자리에 더할 이동량(월드). 옮기지 않으면 Vector3.zero.</returns>
        public static Vector3 Compute(Vector3 expected, Vector3 observed, float minM, float maxM, out bool isClamped)
        {
            isClamped = false;

            Vector3 difference = observed - expected;
            float length = difference.magnitude;
            if (length < minM || length <= 0f)
            {
                return Vector3.zero;
            }

            if (length > maxM)
            {
                isClamped = true;
                return difference / length * maxM;
            }

            return difference;
        }
    }
}
