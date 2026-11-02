using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 쥔 물건이 도구 끝점 기준으로 어디에 어떤 방향으로 붙어 있는지. 쥐면 물건은 도구에 고정되어 함께 움직이므로 이 관계는 변하지 않는다.
    /// 이것을 알면 "물건이 박스의 이 자리에 이 방향으로 놓이려면 도구가 어디서 어느 쪽을 봐야 하는가"를 거꾸로 계산할 수 있다.
    /// 도구를 슬롯 자리로 가져가던 방식은 물건이 도구와 같은 자리에 같은 방향이라고 가정해서, 쥔 자세가 어긋나 있으면 그만큼 어긋나 놓였다.
    /// 물리 엔진이나 씬을 모르고 자세만 다루므로 시험하기 쉽다.
    /// </summary>
    public readonly struct HeldItemFrame
    {
        private HeldItemFrame(Vector3 centerInTool, Quaternion rotationInTool)
        {
            CenterInTool = centerInTool;
            RotationInTool = rotationInTool;
        }

        /// <summary>물건 중심의 도구 기준 좌표.</summary>
        public Vector3 CenterInTool { get; }

        /// <summary>물건 회전의 도구 기준 값.</summary>
        public Quaternion RotationInTool { get; }

        /// <summary>도구의 월드 자세와 물건의 월드 자세를 같은 순간에 재서 둘의 관계를 만든다.</summary>
        public static HeldItemFrame Measure(Pose tool, Vector3 itemCenter, Quaternion itemRotation)
        {
            Quaternion toolInverse = Quaternion.Inverse(tool.rotation);
            return new HeldItemFrame(toolInverse * (itemCenter - tool.position), toolInverse * itemRotation);
        }

        /// <summary>물건이 이 회전으로 놓이려면 도구가 가져야 하는 회전.</summary>
        public Quaternion ToolRotationFor(Quaternion itemRotation)
        {
            return itemRotation * Quaternion.Inverse(RotationInTool);
        }

        /// <summary>도구가 이 회전일 때, 물건 중심이 이 자리에 오려면 도구가 있어야 하는 자리.</summary>
        public Vector3 ToolPositionFor(Vector3 itemCenter, Quaternion toolRotation)
        {
            return itemCenter - toolRotation * CenterInTool;
        }
    }
}
