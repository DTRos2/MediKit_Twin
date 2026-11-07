using System.Globalization;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 툴 카메라 패널에 나오는 글과 글 만들기. 프리팹을 만드는 도구와 화면 코드와 동작 코드가 같은 문구를 쓰도록 한곳에 모았다.
    /// 숫자는 어느 나라 설정에서도 소수점이 점이다. Unity 없이 시험할 수 있다.
    /// </summary>
    public static class ToolCameraPanelTexts
    {
        public const string Title = "툴 카메라";
        public const string Subtitle = "TCP CAM";
        public const string Recording = "REC";

        /// <summary>물건을 찾아보기 전(또는 결과를 보여 주는 시간이 지난 뒤)의 상태 글.</summary>
        public const string Idle = "물건 탐색 안 함 · 집으러 가면 여기에 결과 표시";

        private const float CentimetersPerMeter = 100f;

        /// <summary>카메라가 월드 어디에 있는지를 cm로 적는다. 로봇이 움직이는 동안 값이 따라 바뀐다.</summary>
        public static string CameraPosition(Vector3 positionMeters)
        {
            Vector3 cm = positionMeters * CentimetersPerMeter;
            return string.Format(CultureInfo.InvariantCulture, "x {0:0.0} · y {1:0.0} · z {2:0.0} cm", cm.x, cm.y, cm.z);
        }

        /// <summary>찾은 물건 수 칩 글. 한 번에 하나만 찾으므로 0 아니면 1이다.</summary>
        public static string Detect(bool isDetected)
        {
            return isDetected ? "DETECT 1" : "DETECT 0";
        }
    }
}
