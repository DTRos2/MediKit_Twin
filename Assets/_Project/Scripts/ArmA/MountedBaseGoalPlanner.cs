using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔 베이스를 목표에 놓으려면 베이스 루트가 어디에 어느 방향으로 서야 하는지 한 번에 계산한다. 순수 계산이라 씬 없이 시험한다.
    /// 팔 베이스는 루트에서 떨어져 있어서 루트가 돌면 팔 베이스가 반지름만큼 휘둘린다. 목표를 향해 돌 때마다 팔 오차를 다시 재서
    /// 보정하면 오차가 회전 방향에 수직으로 남아 계속 돌게 되므로, 도착 방향을 먼저 정하고 그 방향에서 팔 베이스가 목표에 오는
    /// 루트 자리를 곧바로 구한다. 루트가 그 자리에서 그 방향으로 서면 팔 베이스는 정확히 목표에 놓인다.
    /// </summary>
    public sealed class MountedBaseGoalPlanner
    {
        private readonly float _minHeadingTravelM;

        /// <param name="minHeadingTravelM">팔 베이스가 이 거리보다 적게 움직이면 방향을 바꾸지 않고 지금 방향으로 선다.</param>
        public MountedBaseGoalPlanner(float minHeadingTravelM)
        {
            if (minHeadingTravelM < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minHeadingTravelM), "거리는 0 이상이어야 한다.");
            }

            _minHeadingTravelM = minHeadingTravelM;
        }

        /// <summary>도착 방향은 이동 방향으로 정한다. 조금만 움직이면 지금 방향을 유지한다.</summary>
        public BaseGoal Plan(Vector3 rootPosition, float rootYawDeg, Vector3 armBasePosition, Vector3 targetArmBase)
        {
            Vector3 travel = Flatten(targetArmBase - armBasePosition);
            float yawDeg = travel.magnitude < _minHeadingTravelM
                ? rootYawDeg
                : Mathf.Atan2(travel.x, travel.z) * Mathf.Rad2Deg;

            return Plan(rootPosition, rootYawDeg, armBasePosition, targetArmBase, yawDeg);
        }

        /// <summary>도착 방향을 호출한 쪽이 정한다. 작업 구역을 정면으로 보고 서게 할 때 쓴다.</summary>
        public BaseGoal Plan(Vector3 rootPosition, float rootYawDeg, Vector3 armBasePosition, Vector3 targetArmBase, float facingYawDeg)
        {
            // 루트 기준으로 본 팔 베이스의 위치. 루트가 돌아도 변하지 않는다.
            Vector3 offsetInRoot = Quaternion.Euler(0f, -rootYawDeg, 0f) * Flatten(armBasePosition - rootPosition);

            Vector3 root = Flatten(targetArmBase) - Quaternion.Euler(0f, facingYawDeg, 0f) * offsetInRoot;
            return new BaseGoal(new Vector3(root.x, rootPosition.y, root.z), facingYawDeg);
        }

        private static Vector3 Flatten(Vector3 vector)
        {
            return new Vector3(vector.x, 0f, vector.z);
        }
    }
}
