using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 박스가 차지하는 영역을 박스 아래의 충돌체로 알아낸다. 바닥판과 벽의 충돌체를 감싸는 축에 맞춘 상자이며,
    /// 물건이 박스 안에 내려앉았는지 튕겨 나갔는지를 가릴 때 쓴다.
    /// 영역의 크기를 코드에 적어 두지 않고 박스의 충돌체에서 읽으므로 박스 모양이 바뀌어도 이 클래스는 그대로다.
    /// 감지용 트리거와 이미 놓인 물건(Graspable)의 충돌체는 박스 모양이 아니므로 뺀다.
    /// </summary>
    public static class BoxFootprint
    {
        /// <summary>박스 아래의 충돌체가 하나도 없으면 false를 돌려주고 bounds는 비어 있다.</summary>
        public static bool TryGetBounds(Transform box, out Bounds bounds)
        {
            bool hasBounds = false;
            bounds = default;

            foreach (Collider collider in box.GetComponentsInChildren<Collider>())
            {
                bool isBoxShape = !collider.isTrigger && collider.GetComponentInParent<Graspable>() == null;
                if (!isBoxShape)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            return hasBounds;
        }

        /// <summary>
        /// 박스 바닥판 윗면의 월드 높이. 박스 모양 충돌체 중 가장 아래에 있는 것의 윗면으로 본다.
        /// 놓인 물건의 밑면이 이보다 얼마나 떠 있는지로, 바닥에 내려앉았는지 허공에서 고정됐는지를 가린다.
        /// </summary>
        public static bool TryGetFloorTopY(Transform box, out float floorTopY)
        {
            bool hasFloor = false;
            float lowestCenterY = 0f;
            floorTopY = 0f;

            foreach (Collider collider in box.GetComponentsInChildren<Collider>())
            {
                bool isBoxShape = !collider.isTrigger && collider.GetComponentInParent<Graspable>() == null;
                if (!isBoxShape)
                {
                    continue;
                }

                Bounds bounds = collider.bounds;
                if (!hasFloor || bounds.center.y < lowestCenterY)
                {
                    lowestCenterY = bounds.center.y;
                    floorTopY = bounds.max.y;
                    hasFloor = true;
                }
            }

            return hasFloor;
        }

        /// <summary>
        /// 월드의 한 점이 박스 영역 안에 있으면 true. 박스 아래에 충돌체가 없어 영역을 알 수 없으면 false.
        /// 박스는 위가 열려 있어 위쪽 한계는 보지 않는다. 벽보다 키가 큰 물건의 중심이 벽 위끝을 넘어도 박스 안에 놓인 것이다.
        /// 바닥판 아래로 내려갔거나 옆으로 벗어났으면 밖이다.
        /// </summary>
        public static bool Contains(Transform box, Vector3 worldPoint)
        {
            if (!TryGetBounds(box, out Bounds bounds))
            {
                return false;
            }

            return worldPoint.x >= bounds.min.x && worldPoint.x <= bounds.max.x
                && worldPoint.z >= bounds.min.z && worldPoint.z <= bounds.max.z
                && worldPoint.y >= bounds.min.y;
        }
    }
}
