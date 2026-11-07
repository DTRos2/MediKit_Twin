using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>박스 슬롯 하나에 구성품을 내려놓을 자리. 놓은 구성품은 박스를 따라 움직이도록 박스(Parent)의 자식이 된다.</summary>
    public readonly struct BoxSlotTarget
    {
        public BoxSlotTarget(Pose worldPose, Transform parent)
        {
            WorldPose = worldPose;
            Parent = parent;
        }

        public Pose WorldPose { get; }

        public Transform Parent { get; }
    }
}
