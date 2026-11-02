using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물건 중심보다 쥐는 자리를 얼마나 높게 겨냥할지 정한다. 순수 계산이라 씬 없이 시험한다.
    /// 그리퍼 손가락 끝은 도구 끝점보다 아래로 내려와 있어서, 판 위에 놓인 얇은 물건의 중심에 도구 끝점을 맞추면
    /// 손가락 끝이 판 아래로 파고들어 팔이 눌린 채 멈춘다. 물건이 얇을수록(중심이 판에 가까울수록) 더 높게 겨냥해야 하고,
    /// 두꺼운 물건은 중심이 이미 판에서 충분히 떠 있으니 그대로 맞춰도 된다.
    /// </summary>
    public static class GraspLift
    {
        /// <param name="fingerDropBelowToolM">손가락 끝이 도구 끝점보다 아래로 내려오는 깊이(m). 0이면 높이지 않는다.</param>
        /// <param name="itemHalfHeightM">물건 높이의 절반(m). 물건 중심에서 판(물건 바닥)까지의 거리다. 모르면 0이다.</param>
        /// <returns>물건 중심에 더할 높이(m). 0 이상이다.</returns>
        public static float Compute(float fingerDropBelowToolM, float itemHalfHeightM)
        {
            return Mathf.Max(0f, fingerDropBelowToolM - Mathf.Max(0f, itemHalfHeightM));
        }
    }
}
