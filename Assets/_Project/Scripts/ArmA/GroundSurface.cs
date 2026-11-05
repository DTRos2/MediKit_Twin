using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 차체가 올라선 바닥을 알아본다: 윗면이 차체 밑면 높이 이하(허용 오차 포함)인 충돌체는 바닥이지 장애물이 아니다.
    /// 차체 상자는 바닥 면에 닿아 있어서, 여유를 두고 키워 따지면 바닥과 항상 겹치기 때문이다. 레일은 바닥 아래까지 뻗어도 윗면이 높으므로 바닥이 아니다.
    /// 씬의 이름이나 레이어를 따지지 않고 높이만 본다.
    /// </summary>
    public static class GroundSurface
    {
        public static bool IsGround(Bounds bounds, float undersideY, float toleranceM)
        {
            return bounds.max.y <= undersideY + toleranceM;
        }

        /// <param name="undersideY">차체에서 가장 낮은 면의 월드 높이(m).</param>
        /// <param name="toleranceM">바닥 윗면이 밑면보다 이만큼(m)까지 높아도 바닥으로 본다.</param>
        public static Predicate<Collider> Of(float undersideY, float toleranceM)
        {
            return collider => IsGround(collider.bounds, undersideY, toleranceM);
        }
    }
}
