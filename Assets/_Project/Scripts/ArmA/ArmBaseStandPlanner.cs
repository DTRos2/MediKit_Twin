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
        private readonly float _approachExtraM;
        private readonly Vector3 _boxStandDirection;

        public ArmBaseStandPlanner(float pickStandoffM, float placeStandoffM, float approachExtraM, Vector3 boxStandDirection)
        {
            if (pickStandoffM < 0f || placeStandoffM < 0f || approachExtraM < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(pickStandoffM), "서는 거리와 접근 여유는 0 이상이어야 한다.");
            }

            _pickStandoffM = pickStandoffM;
            _placeStandoffM = placeStandoffM;
            _approachExtraM = approachExtraM;
            _boxStandDirection = Flatten(boxStandDirection);
        }

        /// <summary>
        /// 선반 물건 앞 자리로 들어가기 전에 거치는 경유지. 서는 자리와 같은 선(칸에서 바깥으로 뻗은 선) 위에서 접근 여유만큼 더 떨어져 있다.
        /// 여기를 거치면 선반과 나란한 방향이 아니라 칸 정면에서 수직으로 들어가므로 베이스가 선반 모서리를 스치지 않는다.
        /// </summary>
        public Vector3 ForShelfItemApproach(Vector3 itemPosition, Vector3 outward, float extraStandoffM = 0f)
        {
            return OnFloor(itemPosition, Flatten(outward), _pickStandoffM + extraStandoffM + _approachExtraM);
        }

        /// <summary>
        /// 선반 물건 앞의 자리. outward는 칸에서 바깥으로 나가는 수평 단위 방향이다.
        /// extraStandoffM은 기본 거리에 더하는 거리로, 팔이 기본 자리에서 닿지 못할 때 더 물러나 다시 시도하는 데 쓴다.
        /// </summary>
        public Vector3 ForShelfItem(Vector3 itemPosition, Vector3 outward, float extraStandoffM = 0f)
        {
            return OnFloor(itemPosition, Flatten(outward), _pickStandoffM + extraStandoffM);
        }

        /// <summary>박스 슬롯 곁의 자리. extraStandoffM은 팔이 닿지 못할 때 더 물러나 다시 서는 거리다.</summary>
        public Vector3 ForBoxSlot(Vector3 slotPosition, float extraStandoffM = 0f)
        {
            return OnFloor(slotPosition, _boxStandDirection, _placeStandoffM + extraStandoffM);
        }

        /// <summary>선반 물건 앞에 섰을 때 물건을 정면으로 보는 방향(월드 Y축 기준 도). offsetDeg는 베이스 정면과 팔 정면이 어긋난 만큼의 보정이다. 방향을 알 수 없으면 null이다.</summary>
        public static float? ShelfItemFacingYawDeg(Vector3 outward, float offsetDeg = 0f)
        {
            return FacingYawDeg(-outward, offsetDeg);
        }

        /// <summary>박스 슬롯 곁에 섰을 때 슬롯을 정면으로 보는 방향(월드 Y축 기준 도). 방향을 알 수 없으면 null이다.</summary>
        public float? BoxSlotFacingYawDeg(float offsetDeg = 0f)
        {
            return FacingYawDeg(-_boxStandDirection, offsetDeg);
        }

        private static float? FacingYawDeg(Vector3 direction, float offsetDeg)
        {
            Vector3 flat = Flatten(direction);
            if (flat.sqrMagnitude < 1e-8f)
            {
                return null;
            }

            return Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg + offsetDeg;
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
