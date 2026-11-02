using System;
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 사람이 알려 준 자세로 집는 순서: 알려 준 자리로 가서(경유지 → 서는 자리), 알려 준 관절 모양에서 출발해 푼 관절 각도로 쥐고 뺀다.
    /// 알려 준 뒤 물건 자리가 수평으로 바뀌었으면 서는 자리와 도구 경로를 그만큼 옮겨 맞춘다(TaughtPoseAnchor). 옮긴 뒤 남는 작은 어긋남은 카메라가 고친다.
    /// 이미 사람이 닿는 것을 확인한 자세이므로 기본으로는 서는 자리나 도구 방향을 찾지 않는다.
    /// 다만 그 자세로 가는 길이 선반 같은 장애물과 겹치면(서는 자리가 알려 준 자리에서 조금 벗어나면 생길 수 있다) 알려 준 방향 가까이에서
    /// 겹치지 않는 방향을 찾고(TaughtClearancePlanner), 그래도 없으면 CollisionRisk로 끝낸다. 그때 서는 자리를 바꿔 다시 찾는 일은 부르는 쪽이 한다.
    /// 그 밖의 실패는 자동 탐색으로 넘어가지 않는다. 알려 준 자세가 안 맞는다는 사실이 가려지기 때문이다(실패 이유에 단계가 남는다).
    /// 관절 각도를 풀 수 없으면(풀이가 없거나 못 풀면) 도구 끝점 자세로 가고, 이때는 겹침을 따지지 않는다.
    /// </summary>
    public sealed class TaughtPickRoutine
    {
        // 이보다 적게 옮기면 측정 오차 수준이라 알리지 않는다(m).
        private const float MinReportedShiftM = 0.01f;

        private readonly StandPolicy _policy;
        private readonly ArmMotionSteps _steps;
        private readonly ShelfGraspSequence _grasp;
        private readonly SeededRouteBuilder _routes;
        private readonly TransferDiagnostics _diagnostics;
        private readonly ShelfPickSettings _settings;
        private readonly Action<string> _logWarning;
        private readonly TaughtClearancePlanner _clearance;
        private readonly TaughtPoseAnchor _anchor;

        /// <param name="seededSolver">출발 자세를 골라 푸는 풀이. 관절 각도로 갈 수 없으면 null이고, 그러면 도구 끝점으로 간다.</param>
        /// <param name="logWarning">경고를 알리는 쪽. 필요 없으면 null.</param>
        /// <param name="clearance">알려 준 자세가 장애물과 겹치면 방향을 바꿔 찾는 쪽. 없으면 알려 준 자세를 그대로 믿는다.</param>
        public TaughtPickRoutine(
            StandPolicy policy,
            ArmMotionSteps steps,
            ShelfGraspSequence grasp,
            ISeededToolSolver seededSolver,
            TransferDiagnostics diagnostics,
            ShelfPickSettings settings,
            Action<string> logWarning,
            TaughtClearancePlanner clearance = null)
        {
            _clearance = clearance;
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _steps = steps ?? throw new ArgumentNullException(nameof(steps));
            _grasp = grasp ?? throw new ArgumentNullException(nameof(grasp));
            _routes = seededSolver != null ? new SeededRouteBuilder(seededSolver) : null;
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            _settings = settings;
            _anchor = new TaughtPoseAnchor(settings.TaughtShiftLimitM);
            _logWarning = logWarning;
        }

        public IEnumerator Run(ItemPickRequest request, TaughtPickPose taught, TransferResult result)
        {
            Vector3 outward = ShelfDirection.Outward(request.Item.ApproachDirection);
            TaughtPickPlan plan = TaughtPickPlan.For(
                taught, outward, _policy.WaypointExtraM, _settings.ApproachDistanceM, _settings.RetreatDistanceM);

            // 알려 준 뒤 물건 자리가 바뀌었으면 서는 자리와 도구 경로를 물건 쪽으로 옮겨 알려 준 자세 그대로 닿게 한다. 남는 높이 차이 같은 작은 어긋남은 카메라가 고친다.
            // 옮길 수 있는 한계보다 멀면 움직이기 전에 알린다(호출하는 쪽이 스스로 찾아 집는다).
            TaughtAnchorResult anchor = _anchor.Anchor(plan, taught.SlotId, request.Item.Pose.position);
            if (!anchor.IsUsable)
            {
                result.Fail(TransferFailure.TaughtPoseStale, anchor.Detail);
                yield break;
            }

            plan = anchor.Plan;
            ReportShift(taught, anchor);

            bool isOk = false;
            if (plan.ArmBaseWaypoint.HasValue)
            {
                yield return _steps.MoveBase(plan.ArmBaseWaypoint, plan.RootYawDeg, TransferSteps.ShelfApproachWaypoint, result, ok => isOk = ok);
                if (!isOk)
                {
                    yield break;
                }
            }

            yield return _steps.MoveBase(plan.ArmBaseStand, plan.RootYawDeg, TransferSteps.ShelfStandPoint, result, ok => isOk = ok);
            if (!isOk)
            {
                yield break;
            }

            // 서는 자리에 도착한 뒤의 팔 베이스 기준으로 알려 준 관절 모양에서 출발해 풀어, 알려 준 관절 각도로 간다.
            TaughtJointRoute route = BuildJointRoute(taught, plan);
            Quaternion rotation = plan.ToolRotation;

            // 그 길이 선반과 겹치면 알려 준 방향 가까이에서 겹치지 않는 방향을 찾는다. 없으면 움직이지 않고 끝낸다(호출하는 쪽이 서는 자리를 바꿔 다시 찾는다).
            if (route != null && _clearance != null)
            {
                var chosen = new ClearGraspPlan();
                yield return _clearance.Plan(taught.JointsRad(), plan, chosen);
                if (!chosen.IsFound)
                {
                    result.Fail(TransferFailure.CollisionRisk, DescribeNoClearGrasp(taught, chosen));
                    yield break;
                }

                ReportClearChoice(taught, chosen);
                rotation = chosen.Rotation;
                route = chosen.Route;
            }

            yield return _grasp.Run(
                request, rotation, plan.NearPosition, plan.GraspPosition, plan.RetreatPosition,
                route, () => ReportJointDeviation(taught), result);
        }

        // 서는 자리를 옮겼으면 알린다. 알려 준 뒤 선반이 바뀐 것이니, 다시 알려 주면 옮기지 않아도 된다.
        private void ReportShift(TaughtPickPose taught, TaughtAnchorResult anchor)
        {
            if (anchor.ShiftM < MinReportedShiftM)
            {
                return;
            }

            _logWarning?.Invoke(
                $"칸 {taught.SlotId}: 알려 준 쥐는 자리가 지금 물건에서 수평으로 {anchor.ShiftM * 100f:0.0}cm 떨어져 있어, " +
                "서는 자리를 그만큼 옮겨 알려 준 자세 그대로 쥔다. 선반이 바뀌었으면 자세를 다시 알려 주면 옮기지 않아도 된다.");
        }

        private static string DescribeNoClearGrasp(TaughtPickPose taught, ClearGraspPlan chosen)
        {
            string where = chosen.FirstBlock.Length > 0 ? $" — 알려 준 방향은 {chosen.FirstBlock}에서 겹친다" : string.Empty;
            return $"{TransferSteps.ShelfStandPoint}: 칸 {taught.SlotId}의 알려 준 자세 근처 {chosen.TriedCount}가지 도구 방향 모두 팔이 장애물과 겹치거나 닿지 않았다{where}";
        }

        private void ReportClearChoice(TaughtPickPose taught, ClearGraspPlan chosen)
        {
            if (chosen.HasCollisionRisk)
            {
                _logWarning?.Invoke(
                    $"칸 {taught.SlotId}: 장애물과 겹치지 않는 도구 방향을 찾지 못해 알려 준 방향으로 간다. 팔이 선반에 닿을 수 있다 — {chosen.FirstBlock}");
            }
            else if (chosen.DeviationDeg > 0f)
            {
                _logWarning?.Invoke(
                    $"칸 {taught.SlotId}: 알려 준 방향이면 {chosen.FirstBlock}에서 겹쳐, 알려 준 방향에서 {chosen.DeviationDeg:0}° 벗어난 방향으로 집는다({chosen.TriedCount}번째 후보).");
            }
        }

        // 알려 준 관절 모양에서 출발해 접근·집기·후퇴 자리의 관절 각도를 푼다. 못 풀면 null이고, 그러면 도구 끝점으로만 간다.
        private TaughtJointRoute BuildJointRoute(TaughtPickPose taught, TaughtPickPlan plan)
        {
            if (_routes == null)
            {
                return null;
            }

            bool isBuilt = _routes.TryBuild(
                taught.JointsRad(),
                new Pose(plan.NearPosition, plan.ToolRotation),
                new Pose(plan.GraspPosition, plan.ToolRotation),
                new Pose(plan.RetreatPosition, plan.ToolRotation),
                out TaughtJointRoute route);
            if (!isBuilt)
            {
                _logWarning?.Invoke(
                    $"칸 {taught.SlotId}의 알려 준 관절 각도로 가는 길을 풀지 못해 도구 끝점으로 간다. 서는 자리가 알려 준 자리에서 많이 벗어났을 수 있다.");
            }

            return route;
        }

        // 알려 준 자세와 다른 관절 값으로 닿았으면 알린다. 같은 도구 자세라도 팔이 다른 모양(해)으로 닿으면 알려 준 곳과 다르게 걸릴 수 있다.
        private void ReportJointDeviation(TaughtPickPose taught)
        {
            string deviation = _diagnostics.JointDeviation(taught.JointsRad());
            if (deviation.Length > 0)
            {
                _logWarning?.Invoke($"칸 {taught.SlotId}에 알려 준 관절 값과 다른 자세로 닿았다(목표 = 알려 준 값) — {deviation}");
            }
        }
    }
}
