using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 박스 슬롯에 놓으러 갈 때 도구가 지나는 세 자리: 슬롯 위, 슬롯, 놓은 뒤 들어올린 자리.
    /// 물건 중심이 슬롯의 위, 슬롯, 들어올린 자리를 지나게 하려는 것이므로, 쥔 물건의 관계(HeldItemFrame)를 알면
    /// 도구 자리를 그만큼 옮겨 계산하고 모르면 도구가 곧 물건 자리라고 본다.
    /// </summary>
    public readonly struct PlacePath
    {
        private PlacePath(Vector3 above, Vector3 slot, Vector3 lift)
        {
            Above = above;
            Slot = slot;
            Lift = lift;
        }

        /// <summary>슬롯 위에서 내려가기 시작하는 도구 자리.</summary>
        public Vector3 Above { get; }

        /// <summary>물건을 놓는 도구 자리.</summary>
        public Vector3 Slot { get; }

        /// <summary>물건을 놓은 뒤 들어올린 도구 자리.</summary>
        public Vector3 Lift { get; }

        /// <summary>같은 방향으로 차례로 지나야 하는 세 자리(슬롯 위, 슬롯, 들어올린 자리). 방향 찾기가 이 자리들에 모두 닿는지 따진다.</summary>
        public Vector3[] Positions => new[] { Above, Slot, Lift };

        /// <param name="frame">쥔 물건과 도구의 관계. 모르면 null이며, 이때는 도구를 물건 중심 자리로 본다.</param>
        /// <param name="slotCenter">물건 중심이 놓여야 하는 슬롯 자리(월드).</param>
        /// <param name="toolRotation">놓을 때 도구의 회전. 도구가 기울면 같은 물건 자리에도 도구 자리가 달라진다.</param>
        /// <param name="releaseHeightM">
        /// 슬롯 중심보다 이만큼 위에서 놓는다(0이면 슬롯 중심에서 놓는다). 좁은 박스에서는 벌린 손가락이 벽 안에 들어가지 못하므로
        /// 손가락이 벽 위에 머물도록 높이 들어 올린 자리에서 놓고, 물건은 아래로 떨어져 슬롯 자리에 내려앉게 한다. 위로 올라가는 접근과 후퇴도 이 높이에서 잰다.
        /// </param>
        public static PlacePath For(
            HeldItemFrame? frame, Vector3 slotCenter, Quaternion toolRotation, float approachHeightM, float liftHeightM, float releaseHeightM = 0f)
        {
            Vector3 release = slotCenter + Vector3.up * releaseHeightM;
            return new PlacePath(
                ToolAt(frame, release + Vector3.up * approachHeightM, toolRotation),
                ToolAt(frame, release, toolRotation),
                ToolAt(frame, release + Vector3.up * liftHeightM, toolRotation));
        }

        private static Vector3 ToolAt(HeldItemFrame? frame, Vector3 itemCenter, Quaternion toolRotation)
        {
            return frame.HasValue ? frame.Value.ToolPositionFor(itemCenter, toolRotation) : itemCenter;
        }
    }
}
