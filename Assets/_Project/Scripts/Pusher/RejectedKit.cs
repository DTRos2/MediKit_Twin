using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Pusher
{
    /// <summary>
    /// 푸셔가 밀어낸 키트 박스에 붙는 표시. 불량 박스가 들어온 박스를 이 표시로 알아보고, 어느 키트인지(KitData) 읽는다.
    /// 푸셔가 밀기 직전에 붙인다. 다른 물체가 불량 박스에 닿아도 이 표시가 없으면 세지 않는다.
    /// </summary>
    public sealed class RejectedKit : MonoBehaviour
    {
        public KitData Kit { get; private set; }

        public void Configure(KitData kit)
        {
            Kit = kit;
        }
    }
}
