using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 로봇암이 쥐고 가는 물건. 물리 조인트 대신 매 프레임 도구 끝점을 따라가게 하는 부착 방식이다. (SRS 2.5: 스크립트 부착)
    /// 선반의 원본은 재고가 줄 때 사라지므로, 쥔 물건은 원본의 복제본이다.
    /// 복제본은 원본이 있던 자리에 만들고, 쥔 순간의 도구 끝점과의 상대 자세를 그대로 지키며 따라간다.
    /// 도구 끝점 자세로 곧바로 옮기면 물건이 손으로 튀어 들어오는 것처럼 보이기 때문이다.
    /// </summary>
    public sealed class CarriedItem
    {
        private readonly Transform _item;
        private readonly Func<Pose> _toolPose;
        private readonly PoseOffset _offsetFromTool;

        private CarriedItem(Transform item, Func<Pose> toolPose, PoseOffset offsetFromTool)
        {
            _item = item;
            _toolPose = toolPose;
            _offsetFromTool = offsetFromTool;
        }

        /// <summary>원본을 복제해 쥔 물건으로 만든다. 원본이 꺼져 있어도 복제본은 켠다.</summary>
        /// <param name="itemPose">쥐는 순간 물건이 있는 월드 자세. 복제본을 여기에 놓는다.</param>
        public static CarriedItem Grab(GameObject prototype, Pose itemPose, Func<Pose> toolPose)
        {
            GameObject clone = UnityEngine.Object.Instantiate(prototype);
            clone.SetActive(true);
            clone.transform.SetPositionAndRotation(itemPose.position, itemPose.rotation);
            return new CarriedItem(clone.transform, toolPose, PoseOffset.Between(toolPose(), itemPose));
        }

        /// <summary>쥔 순간의 상대 자세를 지키며 도구 끝점을 따라간다. 쥐고 있는 동안 매 프레임 부른다.</summary>
        public void Follow()
        {
            Pose pose = _offsetFromTool.ApplyTo(_toolPose());
            _item.SetPositionAndRotation(pose.position, pose.rotation);
        }

        /// <summary>박스 슬롯 자리에 내려놓는다. 박스의 자식이 되어 박스를 따라 움직인다.</summary>
        public void PlaceAt(Pose worldPose, Transform parent)
        {
            _item.SetParent(parent, true);
            _item.SetPositionAndRotation(worldPose.position, worldPose.rotation);
        }

        public void Destroy()
        {
            if (_item != null)
            {
                UnityEngine.Object.Destroy(_item.gameObject);
            }
        }
    }
}
