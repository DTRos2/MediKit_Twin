using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 놓은 물건이 선반에 놓여 있을 때보다 얼마나 기울어 있는지. 선반에서 위를 향하던 물건의 면이 지금도 위를 향하면 0°이고,
    /// 눕혀 있던 물건이 모서리로 서 있으면 90°다. 물건의 종류를 몰라도 "원래 놓여 있던 방식으로 놓였는가"를 가릴 수 있다.
    /// 물건 둘레로 돌아간 각도(위에서 본 방향)는 세지 않는다. 눕혀 놓은 물건은 어느 쪽을 보고 있어도 안정되기 때문이다.
    /// </summary>
    public static class RestingTilt
    {
        /// <param name="restingRotation">물건이 선반에 놓여 있던 회전(월드).</param>
        /// <param name="currentRotation">물건의 지금 회전(월드).</param>
        /// <param name="up">놓인 곳의 위쪽 방향(월드). 박스 바닥이 수평이면 Vector3.up.</param>
        public static float Degrees(Quaternion restingRotation, Quaternion currentRotation, Vector3 up)
        {
            // 선반에서 위를 향하던 물건 쪽 축(물건 좌표)을 구해, 그 축이 지금은 어디를 향하는지 본다.
            Vector3 upAxisInItem = Quaternion.Inverse(restingRotation) * up;
            Vector3 upNow = currentRotation * upAxisInItem;
            return Vector3.Angle(upNow, up);
        }
    }
}
