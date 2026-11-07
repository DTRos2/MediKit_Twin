using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔의 관절 링크 몸체를 알려 주는 쪽. 충돌 상자를 만드는 쪽이 링크에 붙은 충돌체를 찾는 데 쓴다.
    /// 준비되기 전에는 빈 목록이다.
    /// </summary>
    public interface IArmLinkSource
    {
        /// <summary>관절 순서(베이스에서 먼 순서)의 링크 몸체.</summary>
        IReadOnlyList<ArticulationBody> JointBodies { get; }
    }
}
