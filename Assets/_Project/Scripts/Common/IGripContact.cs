using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 그리퍼 손가락이 물건에 실제로 닿았는지 알려 주는 쪽. 닫는 명령을 끝까지 내렸는데 손가락이 끝까지 닫히지 못하고 중간에 멈췄다면
    /// 손가락 사이에 무언가가 있는 것이고, 끝까지 닫혔다면 아무것도 쥐지 못한 것이다.
    /// 물리로 쥐는 쪽이 "물건이 손가락 사이에 있는가"를 확인하고, 손가락 외의 충돌체를 물건에서 떼어 놓는 데 쓴다.
    /// </summary>
    public interface IGripContact
    {
        /// <summary>손가락이 닫히다 물체에 걸려 끝까지 닫히지 못했으면 true. 닫는 명령을 내리고 충분히 기다린 뒤에 읽는다.</summary>
        bool IsBlockedByObject { get; }

        /// <summary>물체와 맞닿는 손가락의 충돌체. 쥔 물건이 부딪혀야 하는 것은 이것뿐이다.</summary>
        IReadOnlyList<Collider> FingerColliders { get; }

        /// <summary>물체를 쥐는 기준점(도구 끝점)의 월드 위치. 쥐는 반경은 이 점에서 잰다.</summary>
        Vector3 GraspCenter { get; }

        /// <summary>쥐는 기준점에서 이 반경(m) 안의 물체를 쥔다.</summary>
        float GraspRadius { get; }
    }
}
