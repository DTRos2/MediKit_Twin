using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>한 물체가 한 자세에서 다른 자세로 옮겨 갈 때의 이동(회전 + 평행 이동)을 월드 변환으로 구한다.</summary>
    public static class RigidMotion
    {
        /// <summary>from 자세를 to 자세로 보내는 월드 변환. 같은 몸체에 붙은 다른 점에 곱하면 그 점도 같이 옮겨진다.</summary>
        public static Matrix4x4 Between(Vector3 fromPosition, Quaternion fromRotation, Vector3 toPosition, Quaternion toRotation)
        {
            Matrix4x4 from = Matrix4x4.TRS(fromPosition, fromRotation, Vector3.one);
            Matrix4x4 to = Matrix4x4.TRS(toPosition, toRotation, Vector3.one);
            return to * from.inverse;
        }
    }
}
