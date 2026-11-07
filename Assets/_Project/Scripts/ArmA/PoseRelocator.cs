using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔이 지금 있는 자리(현재 베이스)에서, 팔이 다른 자리(예정 베이스)에 서 있다고 치고 목표에 닿는지 물어볼 수 있게 목표를 옮긴다.
    /// 예정 베이스에서 본 목표의 위치와 방향을 그대로 두고 현재 베이스 기준으로 다시 놓으면, 지금 팔에게 같은 질문을 해도 같은 답이 나온다.
    /// 순수 계산이라 씬 없이 시험한다.
    /// </summary>
    public static class PoseRelocator
    {
        /// <param name="target">월드 기준 목표 자세.</param>
        /// <param name="currentBase">팔 베이스 링크의 지금 월드 자세.</param>
        /// <param name="plannedBase">팔이 서게 될 때의 팔 베이스 링크 월드 자세.</param>
        public static Pose Relocate(Pose target, Pose currentBase, Pose plannedBase)
        {
            Quaternion inversePlanned = Quaternion.Inverse(plannedBase.rotation);
            Vector3 localPosition = inversePlanned * (target.position - plannedBase.position);
            Quaternion localRotation = inversePlanned * target.rotation;

            return new Pose(
                currentBase.position + currentBase.rotation * localPosition,
                currentBase.rotation * localRotation);
        }
    }
}
