using System;
using System.Collections;
using MediKitTwin.Common;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 그리퍼로 물건을 쥐고 놓는 방식. 이동 순서(ArmItemTransfer)는 그대로 두고 쥐는 방식만 바꿔 끼우기 위한 접점이다.
    /// 복제본을 도구 끝점에 붙이는 방식(ScriptAttachHolder)과 물리 충돌과 조인트로 쥐는 방식(PhysicsGraspHolder)이 있다.
    /// </summary>
    public interface IItemHolder
    {
        /// <summary>접근하기 전에 그리퍼를 연다.</summary>
        void OpenGripper();

        /// <summary>
        /// 선반 물건의 자리에서 그리퍼를 닫아 쥔다. 쥐었으면 done(true), 쥐지 못했으면 done(false)를 부른다.
        /// 재고를 줄인 뒤(선반 원본이 사라진 뒤)에 부르므로 원본이 아니라 복제본을 쥔다.
        /// </summary>
        IEnumerator Grasp(ShelfPickItem item, Action<bool> done);

        /// <summary>쥔 물건을 박스 슬롯 자리에 내려놓는다. 내려놓은 물건은 박스의 자식이 된다.</summary>
        IEnumerator Release(ItemPlaceRequest request);

        /// <summary>쥔 물건을 버린다. 쥔 것이 없으면 아무것도 하지 않는다.</summary>
        void Discard();
    }
}
