using System.Globalization;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 베이스의 자리를 Console과 패널에 적을 한 줄 글자로 만든다. 찾은 자리를 설정값(서는 거리 등)에 옮겨 적을 때 그대로 읽을 수 있게,
    /// 위치는 소수점 둘째 자리(cm), 방향은 소수점 첫째 자리까지 적는다. 글자 만드는 일만 하므로 씬 없이 시험한다.
    /// </summary>
    public static class BasePoseReport
    {
        /// <param name="root">베이스 루트(굴러가는 몸체)의 자리.</param>
        /// <param name="armBase">팔 베이스(어깨)의 월드 위치. 알 수 없으면 null이다.</param>
        public static string Format(BasePlacement root, Vector3? armBase)
        {
            string text = $"루트 x {Number(root.Position.x)}  z {Number(root.Position.z)}  방향 {root.YawDeg.ToString("0.0", CultureInfo.InvariantCulture)}°";

            return armBase.HasValue
                ? $"{text} | 팔 베이스 x {Number(armBase.Value.x)}  z {Number(armBase.Value.z)}"
                : text;
        }

        private static string Number(float meters)
        {
            return meters.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
