using System.Collections;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 구성품 하나를 선반에서 집어 박스에 놓는 동작. 키팅 진행(KittingProcess)은 이 인터페이스만 보므로,
    /// 실제 로봇암, 물리 파지, 시험용 가짜 구현을 바꿔 끼워도 진행 순서는 그대로다.
    /// 코루틴 형태이므로 로봇이 움직이는 동안 호출하는 쪽이 기다린다.
    /// </summary>
    public interface IItemTransfer
    {
        /// <summary>선반의 물건 위로 접근해 쥐고(쥐는 순간 TryCommit 호출) 들어올린다.</summary>
        IEnumerator Pick(ItemPickRequest request, TransferResult result);

        /// <summary>쥐고 있는 물건을 박스 슬롯 위로 옮겨 내려놓고 물러난다.</summary>
        IEnumerator Place(ItemPlaceRequest request, TransferResult result);

        /// <summary>쥐고 있는 물건을 버린다. 키팅을 중간에 멈출 때 부른다. 쥔 것이 없으면 아무것도 하지 않는다.</summary>
        void Discard();
    }
}
