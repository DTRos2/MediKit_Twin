using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 로봇암이 쥐고 가는 물건. 물리 조인트 대신 매 프레임 도구 끝점을 따라가게 하는 부착 방식이다. (SRS 2.5: 스크립트 부착)
    /// 선반의 원본은 재고가 줄 때 사라지므로, 쥔 물건은 원본의 복제본이다.
    /// </summary>
    public sealed class CarriedItem
    {
        private readonly Transform _item;
        private readonly Func<Pose> _toolPose;

        private CarriedItem(Transform item, Func<Pose> toolPose)
        {
            _item = item;
            _toolPose = toolPose;
        }

        /// <summary>원본을 복제해 쥔 물건으로 만든다. 원본이 꺼져 있어도 복제본은 켠다.</summary>
        public static CarriedItem Grab(GameObject prototype, Func<Pose> toolPose)
        {
            GameObject clone = UnityEngine.Object.Instantiate(prototype);
            clone.SetActive(true);
            return new CarriedItem(clone.transform, toolPose);
        }

        /// <summary>도구 끝점 자세로 옮긴다. 쥐고 있는 동안 매 프레임 부른다.</summary>
        public void Follow()
        {
            Pose tool = _toolPose();
            _item.SetPositionAndRotation(tool.position, tool.rotation);
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
