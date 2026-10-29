using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 도구 끝점 자세를 관절 각도로 바꾼다. 로봇 종류(FR5, UR3)와 풀이 방법(DLS, 해석해)은 구현체가 안다.
    /// </summary>
    public interface IInverseKinematics
    {
        /// <param name="targetInBase">로봇 베이스 좌표계 기준 목표 자세</param>
        /// <param name="seedRad">풀이를 시작할 관절 각도(rad). 현재 자세를 주면 가까운 해를 찾는다.</param>
        /// <param name="resultRad">결과 관절 각도(rad). seedRad와 길이가 같아야 한다.</param>
        IkResult Solve(Pose targetInBase, float[] seedRad, float[] resultRad);
    }
}
