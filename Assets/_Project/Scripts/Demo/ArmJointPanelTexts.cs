using System.Globalization;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 관절 제어 패널에 나오는 글과 글 만들기. 프리팹을 만드는 도구와 화면 코드와 동작 코드가 같은 문구를 쓰도록 한곳에 모았다.
    /// 숫자는 어느 나라 설정에서도 소수점이 점이다. Unity 없이 시험할 수 있다.
    /// </summary>
    public static class ArmJointPanelTexts
    {
        public const string Title = "관절 제어";
        public const string Subtitle = "ARM JOINTS";

        public const string NotReady = "로봇이 준비되는 중...";

        public const string ManualTitle = "수동 제어";
        public const string ManualDescription = "켜면 자동 키팅이 멈추고 팔이 그 자리에 남음";
        public const string HomePose = "시작 자세로";
        public const string MatchCurrent = "지금 자세에 맞춤";

        public const string KeyboardTitle = "키보드 조작";
        public const string KeyboardDescription = "글자 입력 중에는 꺼짐";

        // 키 안내: 키 모양 칸의 글과 그 키가 하는 일.
        public const string MoveKeys = "↑↓←→";
        public const string MoveMeaning = "앞뒤·옆 이동";
        public const string TurnKeys = ", .";
        public const string TurnMeaning = "회전";
        public const string FastKey = "R-Shift";
        public const string FastMeaning = "빠르게";
        public const string LogKey = "Enter";
        public const string LogMeaning = "위치 출력";

        public const string TaughtHeading = "집는 자세 저장";
        public const string TaughtDescription = "지금 자세를 선반 칸의 집는 자세로 저장한다 (ArmA 자리·방향 + 도구 + 관절)";
        public const string SlotLabel = "칸";
        public const string Save = "저장";
        public const string Load = "불러오기";
        public const string Remove = "삭제";

        /// <summary>칸 ID 입력칸의 처음 글과 최대 글자 수.</summary>
        public const string DefaultSlotId = "A-1";
        public const int SlotIdMaxLength = 8;

        /// <summary>관절 줄을 미리 놓는 개수. 로봇의 관절 수가 이보다 많으면 앞의 이만큼만 보인다(6축 팔 기준 여유를 두었다).</summary>
        public const int JointRowCapacity = 8;

        /// <summary>관절 값 글(도). 예: 12.5°</summary>
        public static string JointValue(float degrees)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.0}°", degrees);
        }

        public static string ToolPosition(Vector3 position)
        {
            return string.Format(
                CultureInfo.InvariantCulture, "도구 끝점(월드): x {0:0.00}  y {1:0.00}  z {2:0.00} m", position.x, position.y, position.z);
        }

        /// <summary>접근 방향(+Z). 칸 안으로 들어가는 방향이 얼마나 기울었는지 읽는 데 쓴다.</summary>
        public static string ToolApproach(Vector3 approach)
        {
            return string.Format(
                CultureInfo.InvariantCulture, "도구 접근 방향(+Z): x {0:+0.00;-0.00}  y {1:+0.00;-0.00}  z {2:+0.00;-0.00}",
                approach.x, approach.y, approach.z);
        }

        /// <summary>월드 오일러각. 손목을 얼마나 돌렸는지 읽는 데 쓴다. 0~360이 아니라 -180~180으로 적는다.</summary>
        public static string ToolEuler(Vector3 eulerDegrees)
        {
            return string.Format(
                CultureInfo.InvariantCulture, "도구 방향(월드 각): x {0:0}°  y {1:0}°  z {2:0}°",
                Mathf.DeltaAngle(0f, eulerDegrees.x), Mathf.DeltaAngle(0f, eulerDegrees.y), Mathf.DeltaAngle(0f, eulerDegrees.z));
        }
    }
}
