using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>쥔 구성품을 박스 슬롯에 내려놓아 달라는 요청. 내려놓은 구성품은 박스(Parent)의 자식이 되어 박스를 따라 움직인다.</summary>
    public readonly struct ItemPlaceRequest
    {
        public ItemPlaceRequest(Pose worldPose, Transform parent)
        {
            WorldPose = worldPose;
            Parent = parent;
        }

        public Pose WorldPose { get; }

        public Transform Parent { get; }
    }
}
