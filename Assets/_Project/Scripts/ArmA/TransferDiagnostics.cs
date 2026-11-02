using System;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔이 목표에 닿지 못했을 때 왜 못 닿았는지 설명하는 글을 만든다. 이송 순서(ArmItemTransfer)와 동작 단계(ArmMotionSteps)는
    /// 실패했다는 사실만 알고, 어떻게 설명할지는 여기서 정한다. 읽기만 하고 아무것도 움직이지 않는다.
    /// 설명을 도울 수 있는 부가 정보(IToolTrackingDiagnostics, IToolReachFrame)가 없는 로봇이면 있는 정보만으로 설명한다.
    /// </summary>
    public sealed class TransferDiagnostics
    {
        private const string HeldMessage = "수동 제어가 켜져 있어 자동 동작을 멈췄다";

        private readonly IToolPoseTarget _tool;
        private readonly IToolTrackingDiagnostics _tracking;
        private readonly IToolReachFrame _reachFrame;
        private readonly Func<bool> _isHeld;
        private readonly float _positionToleranceM;

        /// <param name="tool">도구 끝점 목표를 받는 쪽.</param>
        /// <param name="tracking">목표를 못 따라간 관절을 설명해 주는 쪽. 없으면 null.</param>
        /// <param name="reachFrame">팔 베이스에서 본 지점의 거리를 알려 주는 쪽. 없으면 null.</param>
        /// <param name="isHeld">사람이 팔을 직접 조작하느라 자동 동작이 막혀 있으면 true를 돌려주는 함수.</param>
        /// <param name="positionToleranceM">닿았다고 보는 위치 오차(m). 닿기 직전까지는 갔는데 못 닿은 경우를 가려내는 데 쓴다.</param>
        public TransferDiagnostics(
            IToolPoseTarget tool, IToolTrackingDiagnostics tracking, IToolReachFrame reachFrame, Func<bool> isHeld, float positionToleranceM)
        {
            _tool = tool ?? throw new ArgumentNullException(nameof(tool));
            _tracking = tracking;
            _reachFrame = reachFrame;
            _isHeld = isHeld ?? throw new ArgumentNullException(nameof(isHeld));
            _positionToleranceM = positionToleranceM;
        }

        /// <summary>도구 끝점 목표에 닿지 못한 이유.</summary>
        public string ToolFailure()
        {
            if (_isHeld())
            {
                return HeldMessage;
            }

            if (_tool.LastSolveSucceeded)
            {
                return $"팔이 시간 안에 목표 자세에 닿지 못했다(무언가에 걸렸거나 너무 느림){TrackingSuffix()}";
            }

            IkResult ik = _tool.LastResult;
            return $"팔이 닿는 자세를 찾지 못했다(IK 위치 오차 {ik.PositionError:F3}m, 회전 오차 {ik.RotationErrorDeg:F1}도)";
        }

        /// <summary>관절 각도 목표에 닿지 못한 이유. 목표와 5도 이상 다른 관절이 있으면 그 관절을 적는다.</summary>
        public string JointFailure(IReadOnlyList<float> targetRad)
        {
            if (_isHeld())
            {
                return HeldMessage;
            }

            const string Base = "팔이 시간 안에 알려 준 관절 각도에 닿지 못했다(무언가에 걸렸거나 너무 느림)";
            string deviation = JointDeviation(targetRad);
            return deviation.Length == 0 ? Base : $"{Base} — 관절 {deviation}";
        }

        /// <summary>
        /// 지금 관절 각도가 기준 각도와 얼마나 다른지. 같은 도구 자세에 다른 관절 모양으로 닿았는지 알아내는 데 쓴다.
        /// 거의 같거나 비교할 수 없으면 빈 글자다.
        /// </summary>
        public string JointDeviation(IReadOnlyList<float> referenceRad)
        {
            return _tracking != null ? _tracking.DescribeJointDeviation(referenceRad) : string.Empty;
        }

        /// <summary>
        /// 주어진 도구 방향으로 각 지점에 닿는 정도. 오차가 크면 팔이 닿는 범위 밖이고,
        /// 오차가 거의 없는데 못 닿았다면 현재 자세에서 너무 멀리 돌아야만 닿는 것이다(IK의 관절 이동 제한).
        /// </summary>
        public string Reach(IReadOnlyList<Vector3> positions, Quaternion rotation)
        {
            var parts = new string[positions.Count];
            for (int i = 0; i < parts.Length; i++)
            {
                IkResult ik = _tool.Probe(new Pose(positions[i], rotation));
                bool isFarOnly = !ik.Converged && ik.PositionError <= _positionToleranceM;
                string reach = isFarOnly
                    ? "현재 자세에서 너무 멀리 돌아야 닿음"
                    : $"오차 {ik.PositionError:F2}m/{ik.RotationErrorDeg:F0}도";
                parts[i] = $"지점{i + 1}: {reach}{Placement(positions[i])}";
            }

            return string.Join(", ", parts);
        }

        // 목표를 못 따라간 정도를 덧붙인다. 한계에 붙은 관절이면 한계 문제이고, 한계에서 먼데도 못 따라간 관절이면 무언가에 걸린 것이다.
        private string TrackingSuffix()
        {
            string detail = _tracking != null ? _tracking.DescribeTracking() : string.Empty;
            return detail.Length == 0 ? string.Empty : $" — {detail}";
        }

        // 팔 베이스에서 본 지점의 수평 거리와 높이 차. 팔의 닿는 반경과 견주어 거리 문제인지 높이 문제인지 가려내는 데 쓴다.
        private string Placement(Vector3 worldPoint)
        {
            if (_reachFrame == null)
            {
                return string.Empty;
            }

            Vector3 offset = _reachFrame.OffsetFromArmBase(worldPoint);
            float horizontal = new Vector2(offset.x, offset.z).magnitude;
            return $" [팔 베이스에서 수평 {horizontal:F2}m, 높이 {offset.y:+0.00;-0.00}m]";
        }
    }
}
