using UnityEngine;

namespace MediKitTwin.Inspection
{
    /// <summary>
    /// 검사 카메라가 박스를 내려다볼 때 이미지의 위쪽이 가리킬 수평 방향을 정한다.
    /// 학습 이미지는 "이미지의 위쪽이 박스의 +Z"로 찍었으므로, 박스가 컨베이어에서 어느 방향으로 들어오든(코너를 지나면 90°씩 돈다)
    /// 카메라를 박스의 +Z에 맞춰 돌려야 슬롯 크롭이 슬롯 자리에 맞는다. 카메라를 마운트 방향으로 고정하면 박스가 돌아 들어올 때
    /// 크롭이 박스 밖이나 다른 슬롯을 찍어 CNN이 정상 키트도 불량으로 판정한다.
    /// </summary>
    public static class BoxImageUp
    {
        // 박스의 +Z가 거의 수직(눕거나 뒤집힘)이면 수평 방향이 정해지지 않는다. 이보다 작으면 정해진 방향이 없는 것으로 본다.
        private const float MinFlatLength = 0.1f;

        /// <param name="boxForward">박스의 +Z(월드).</param>
        /// <param name="fallbackUp">박스의 +Z에서 수평 방향을 못 구할 때 쓸 이미지 위쪽(보통 마운트가 향한 수평 방향).</param>
        /// <returns>이미지 위쪽이 향할 수평 단위 벡터.</returns>
        public static Vector3 From(Vector3 boxForward, Vector3 fallbackUp)
        {
            Vector3 flat = Vector3.ProjectOnPlane(boxForward, Vector3.up);
            return flat.magnitude >= MinFlatLength ? flat.normalized : fallbackUp;
        }
    }
}
