using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 팔 베이스를 기준으로 월드의 한 점이 어디 있는지 알려 준다. 닿지 않는 이유가 거리 때문인지 높이 때문인지 알리는 데 쓴다.
    /// IToolPoseTarget과 따로 둔 것은 이 정보가 없는 구현체(ROS 등)도 도구 목표를 받을 수 있게 하기 위해서다.
    /// </summary>
    public interface IToolReachFrame
    {
        /// <summary>팔 베이스에서 worldPoint까지의 월드 기준 벡터.</summary>
        Vector3 OffsetFromArmBase(Vector3 worldPoint);
    }
}
