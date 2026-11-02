using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 베이스 루트가 정해진 방향으로 서서 팔 베이스를 목표 지점에 놓았을 때, 팔 베이스 링크의 월드 자세를 미리 구한다.
    /// 팔 베이스는 루트에 붙어 함께 움직이므로 위치는 목표 지점이 되고(높이는 그대로), 방향은 루트가 돈 만큼 수평으로 돈다.
    /// MountedBaseGoalPlanner가 루트 자리를 구하는 것과 같은 가정을 쓴다. 순수 계산이라 씬 없이 시험한다.
    /// </summary>
    public static class ArmBasePosePredictor
    {
        /// <param name="currentArmBase">팔 베이스 링크의 지금 월드 자세.</param>
        /// <param name="currentRootYawDeg">베이스 루트의 지금 방향(도, 월드 Y축 기준).</param>
        /// <param name="targetArmBasePosition">팔 베이스를 놓을 지점(월드). 높이는 쓰지 않는다.</param>
        /// <param name="facingYawDeg">도착했을 때 루트가 보는 방향(도, 월드 Y축 기준).</param>
        public static Pose Predict(Pose currentArmBase, float currentRootYawDeg, Vector3 targetArmBasePosition, float facingYawDeg)
        {
            Quaternion turn = Quaternion.Euler(0f, facingYawDeg - currentRootYawDeg, 0f);
            var position = new Vector3(targetArmBasePosition.x, currentArmBase.position.y, targetArmBasePosition.z);

            return new Pose(position, turn * currentArmBase.rotation);
        }
    }
}
