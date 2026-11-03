using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>쥔 구성품을 박스 슬롯에 내려놓아 달라는 요청. 내려놓은 구성품은 박스(Parent)의 자식이 되어 박스를 따라 움직인다.</summary>
    public readonly struct ItemPlaceRequest
    {
        /// <param name="itemKey">
        /// 어떤 구성품인지 알아보는 이름(선반 칸 이름 등). 같은 슬롯이라도 구성품마다 쥔 자세가 달라 놓는 방식이 다를 수 있어,
        /// 성공한 놓기 방식을 기억할 때 이 이름으로 가른다. 모르면 비워 둔다.
        /// </param>
        public ItemPlaceRequest(Pose worldPose, Transform parent, string itemKey = "")
        {
            WorldPose = worldPose;
            Parent = parent;
            ItemKey = itemKey ?? string.Empty;
        }

        public Pose WorldPose { get; }

        public Transform Parent { get; }

        public string ItemKey { get; }
    }
}
