using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Unloader
{
    /// <summary>
    /// 하역 로봇이 싣고 다니는 박스에 붙어 어느 키트의 박스인지 알려 준다. (FR-12)
    /// 컨베이어 위에서 키트를 들고 있던 받침(KitBoxCarrier)은 박스가 받침대에 등록될 때 없어지므로, 그 뒤 출하(KitShipped)까지 이 컴포넌트가 키트를 들고 다닌다.
    /// 받침과 같은 IKitCarrier라서, 받침대에 박스가 남아 있는지 보는 쪽(컨베이어 끝)이 받침이 사라진 뒤에도 이 박스를 알아본다.
    /// </summary>
    public sealed class KitCargo : MonoBehaviour, IKitCarrier
    {
        public KitData Kit { get; private set; }

        public Transform Box => transform;

        public void Bind(KitData kit)
        {
            Kit = kit;
        }
    }
}
