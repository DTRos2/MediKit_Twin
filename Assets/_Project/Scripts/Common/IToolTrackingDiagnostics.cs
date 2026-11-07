using System.Collections.Generic;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 도구 끝점 목표를 따라가다 멈췄을 때, 무엇이 목표를 따라가지 못했는지 설명하는 쪽.
    /// 남은 도구 끝점 오차와, 목표 각도를 따라가지 못한 관절이 한계에 붙었는지(한계 문제) 한계에서 먼지(무언가에 걸림)를 알려 준다.
    /// </summary>
    public interface IToolTrackingDiagnostics
    {
        /// <summary>목표를 따라가지 못한 정도를 설명한 글자. 설명할 목표나 풀이가 없으면 빈 글자다.</summary>
        string DescribeTracking();

        /// <summary>
        /// 지금 관절 각도(rad, URDF 기준)가 기준 각도와 얼마나 다른지 설명한 글자. 기준과 거의 같으면(또는 비교할 수 없으면) 빈 글자다.
        /// 같은 도구 자세에 다른 관절 모양으로 닿았는지 알아내는 데 쓴다.
        /// </summary>
        string DescribeJointDeviation(IReadOnlyList<float> referenceRad);
    }
}
