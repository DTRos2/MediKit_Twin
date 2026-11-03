using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 받침(KitBoxCarrier) 위에 얹힌 키트 박스를 컨베이어에서 풀어 주는 중계. (FR-05, FR-09)
    /// 푸셔는 박스 오브젝트에서 IBoxConveyorRelease를 찾는데, 컨베이어가 붙잡고 있는 것은 박스가 아니라 그 부모인 받침이다.
    /// 그래서 박스에 이 컴포넌트를 두고, 부르면 받침을 컨베이어에서 풀고 박스만 꺼낸 뒤 빈 받침을 지운다.
    /// 이미 풀렸으면(받침이 없으면) 아무것도 하지 않는다.
    /// 박스 안 물건은 키네마틱 Rigidbody로 박스를 따라다닌다. 키네마틱은 물리 스텝 안에서 스스로 움직이지 않으므로,
    /// 박스를 물리로 풀어 밀면 박스가 자기 안의 물건과의 접촉에 막혀 꼼짝하지 않는다(푸셔 헤드가 박스를 통과해 버린다).
    /// 그래서 풀 때 물건의 충돌체가 아무것과도 부딪히지 않게 하고, 물건은 박스의 자식으로 따라가기만 하게 한다.
    /// </summary>
    public sealed class KitBoxConveyorRelease : MonoBehaviour, IBoxConveyorRelease
    {
        // 제외 레이어를 전부로 하면 그 충돌체는 어떤 레이어와도 접촉을 만들지 않는다.
        private const int AllLayers = ~0;

        public void ReleaseToPhysics()
        {
            KitBoxParcel carrierParcel = GetComponentInParent<KitBoxParcel>();
            if (carrierParcel == null)
            {
                return;
            }

            carrierParcel.ReleaseToPhysics();

            // 박스를 받침에서 꺼내 월드에 둔다. 월드 위치와 자세는 그대로라 박스가 움직여 보이지 않는다.
            transform.SetParent(null, true);
            Destroy(carrierParcel.gameObject);

            LetContentsRideWithoutColliding();
        }

        // 박스 자신의 Rigidbody(푸셔가 붙인다)가 아닌, 안에 놓인 물건들의 Rigidbody 아래 충돌체를 접촉에서 뺀다.
        private void LetContentsRideWithoutColliding()
        {
            foreach (Rigidbody body in GetComponentsInChildren<Rigidbody>(true))
            {
                if (body.transform == transform)
                {
                    continue;
                }

                foreach (Collider collider in body.GetComponentsInChildren<Collider>(true))
                {
                    collider.excludeLayers = AllLayers;
                }
            }
        }
    }
}
