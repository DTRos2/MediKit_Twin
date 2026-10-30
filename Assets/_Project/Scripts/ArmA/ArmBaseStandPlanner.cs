using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물건을 집거나 놓을 때 팔 베이스가 서야 할 자리를 정한다. 순수 계산이라 씬 없이 시험한다.
    /// 선반 물건은 칸 밖으로 나가는 방향으로, 박스 슬롯은 정해진 방향으로 각각 거리만큼 떨어진 바닥 지점에 선다.
    /// 거리와 방향은 설정(ArmBaseApproachConfigSO)에서 받으므로 이 클래스에는 숫자가 없다.
    /// </summary>
    public sealed class ArmBaseStandPlanner
    {
        private readonly float _pickStandoffM;
        private readonly float _placeStandoffM;
        private readonly Vector3 _boxStandDirection;

        public ArmBaseStandPlanner(float pickStandoffM, float placeStandoffM, Vector3 boxStandDirection)
        {
            if (pickStandoffM < 0f || placeStandoffM < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(pickStandoffM), "서는 거리는 0 이상이어야 한다.");
            }

            _pickStandoffM = pickStandoffM;
            _placeStandoffM = placeStandoffM;
            _boxStandDirection = Flatten(boxStandDirection);
        }

        /// <summary>선반 물건 앞의 자리. outward는 칸에서 바깥으로 나가는 수평 단위 방향이다.</summary>
        public Vector3 ForShelfItem(Vector3 itemPosition, Vector3 outward)
        {
            return OnFloor(itemPosition, Flatten(outward), _pickStandoffM);
        }

        /// <summary>박스 슬롯 곁의 자리.</summary>
        public Vector3 ForBoxSlot(Vector3 slotPosition)
        {
            return OnFloor(slotPosition, _boxStandDirection, _placeStandoffM);
        }

        private static Vector3 OnFloor(Vector3 origin, Vector3 direction, float distance)
        {
            return new Vector3(origin.x + direction.x * distance, 0f, origin.z + direction.z * distance);
        }

        // 수평 성분만 쓰고 길이를 1로 맞춘다. 영벡터면 그대로 영벡터(제자리)이다.
        private static Vector3 Flatten(Vector3 direction)
        {
            var flat = new Vector3(direction.x, 0f, direction.z);
            return flat.sqrMagnitude < 1e-8f ? Vector3.zero : flat.normalized;
        }
    }
}
