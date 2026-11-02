using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 알려 준 쥐는 자리를 지금 물건 자리에 수평으로 맞춘다. 알려 준 자세는 월드 좌표라서, 알려 준 뒤 선반이나 칸 모양이 바뀌어 물건이 옮겨지면
    /// 그 자세 그대로는 쥘 수 없다(팔이 닿는 거리 밖이거나 물건에 못 미친다). 이때 서는 자리와 도구 경로를 물건이 옮겨진 만큼 함께 옮기면,
    /// 베이스와 도구 끝점의 상대 위치(알려 준 관절 모양)가 그대로라 알려 준 자세로 닿는다. 높이는 베이스가 바꿀 수 없으므로 맞추지 않고 카메라가 고친다.
    /// 옮길 수 있는 거리에 한계를 둔다. 한계보다 멀면 다른 칸이나 크게 바뀐 선반이라 쓰지 않는다(호출하는 쪽이 정한다).
    /// 한계가 0 이하이면 옮기지 않고 저장값 그대로 쓴다. 순수 계산이라 씬 없이 시험한다.
    /// </summary>
    public readonly struct TaughtPoseAnchor
    {
        private readonly float _limitM;

        /// <param name="limitM">서는 자리를 옮길 수 있는 최대 수평 거리(m). 0 이하이면 옮기지 않고 저장값 그대로 쓴다.</param>
        public TaughtPoseAnchor(float limitM)
        {
            _limitM = limitM;
        }

        public TaughtAnchorResult Anchor(TaughtPickPlan plan, string slotId, Vector3 itemCenter)
        {
            if (_limitM <= 0f)
            {
                return TaughtAnchorResult.Usable(plan, 0f);
            }

            Vector3 delta = plan.HorizontalDeltaTo(itemCenter);
            float shiftM = delta.magnitude;
            if (shiftM > _limitM)
            {
                string detail =
                    $"칸 {slotId}의 알려 준 쥐는 자리가 지금 물건에서 수평으로 {shiftM * 100f:0.0}cm 떨어져 있다(옮길 수 있는 한계 {_limitM * 100f:0.0}cm). " +
                    "다른 칸이거나 알려 준 뒤 선반이 많이 바뀐 것 같다";
                return TaughtAnchorResult.Unusable(plan, shiftM, detail);
            }

            return TaughtAnchorResult.Usable(plan.Shifted(delta), shiftM);
        }
    }
}
