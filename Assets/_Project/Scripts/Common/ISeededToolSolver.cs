using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 정해 준 관절 모양에서 출발해 도구 끝점이 월드의 한 자세에 닿는 가까운 관절 각도를 풀어 본다. 팔은 움직이지 않는다.
    /// IToolPoseTarget.Probe는 지금 자세에서 출발하지만, 이쪽은 출발 자세를 고를 수 있어 "알려 준 자세와 같은 모양"의 해를 얻는 데 쓴다.
    /// </summary>
    public interface ISeededToolSolver
    {
        /// <param name="worldPose">닿아야 할 도구 끝점의 월드 자세. 접근 방향이 +Z다.</param>
        /// <param name="seedRad">풀이를 시작할 관절 각도(rad).</param>
        /// <param name="solutionRad">결과를 받을 배열. 관절 수와 길이가 같아야 한다.</param>
        /// <returns>닿는 해를 찾았으면 true. 못 찾았거나 길이가 맞지 않으면 false.</returns>
        bool TrySolveFrom(Pose worldPose, IReadOnlyList<float> seedRad, float[] solutionRad);
    }
}
