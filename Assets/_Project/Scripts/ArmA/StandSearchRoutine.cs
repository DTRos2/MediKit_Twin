using System;
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 서서 팔이 닿는 도구 방향을 찾는 절차. 선반에서 집기와 박스에 놓기가 ReachPlan만 바꿔 같은 절차를 쓴다.
    /// 가능하면 이동하기 전에 서 볼 자리마다 닿는 방향을 가늠해(StandPlacementSearcher) 점수가 좋은 자리부터 서 보고,
    /// 가늠할 수 없거나 가늠으로는 닿는 자리가 하나도 없으면 서는 거리를 계획의 보정값만큼 차례로 늘려 서 본다.
    /// 베이스가 못 가는 경우는 자리를 바꿔도 소용없으므로 곧바로 끝낸다. 실패하면 결과에 이유가 남고 done에는 못 찾은 결과를 넘긴다.
    /// 가늠은 방향을 하나씩 확인해 시간이 걸리므로(시간 예산 FrameBudget 안에서 프레임마다 이어서 한다), 시작과 끝을 알려 어디서 기다리는지 보이게 한다.
    /// 움직임 한 단계는 ArmMotionSteps, 이유 설명은 TransferDiagnostics가 맡는다.
    /// </summary>
    public sealed class StandSearchRoutine
    {
        private readonly ArmMotionSteps _steps;
        private readonly GraspOrientationSearcher _searcher;
        private readonly StandPlacementCost _cost;
        private readonly Func<Pose, bool> _canReachForPlanning;
        private readonly IArmBaseStateSource _baseState;
        private readonly TransferDiagnostics _diagnostics;
        private readonly StandSearchSettings _settings;
        private readonly Func<float> _now;
        private readonly Action<string> _log;
        private readonly Func<Pose, Pose, bool> _planningIsClear;
        private readonly ClearancePolicy _clearancePolicy;
        private readonly FrameBudget _budget;

        /// <param name="searcher">서는 자리에 도착한 뒤 실제 팔로 닿는 방향을 찾는 쪽.</param>
        /// <param name="canReachForPlanning">서 보기 전에 가늠할 때 쓰는 도달 판정(월드 기준 도구 자세 → 닿는가).</param>
        /// <param name="baseState">팔 베이스의 지금 자세를 알려 주는 쪽. 없으면 미리 가늠하지 못한다.</param>
        /// <param name="now">경과 시간을 재는 시계.</param>
        /// <param name="log">진행을 알리는 쪽. 필요 없으면 null.</param>
        /// <param name="planningIsClear">서 보기 전에 가늠할 때 쓰는 겹침 판정((지금 베이스 기준으로 옮긴 도구 자세, 후보 자리의 팔 베이스 자세) → 겹치지 않는가). 겹침을 따지지 않으면 null.</param>
        /// <param name="clearancePolicy">겹치지 않는 방향이 없을 때의 규칙. null이면 기본 규칙.</param>
        /// <param name="budget">서 보기 전에 가늠할 때 한 프레임에 이어서 따질 수 있는 시간. null이면 후보 하나마다 한 프레임을 쉰다.</param>
        public StandSearchRoutine(
            ArmMotionSteps steps,
            GraspOrientationSearcher searcher,
            StandPlacementCost cost,
            Func<Pose, bool> canReachForPlanning,
            IArmBaseStateSource baseState,
            TransferDiagnostics diagnostics,
            StandSearchSettings settings,
            Func<float> now,
            Action<string> log,
            Func<Pose, Pose, bool> planningIsClear = null,
            ClearancePolicy clearancePolicy = null,
            FrameBudget budget = null)
        {
            _steps = steps ?? throw new ArgumentNullException(nameof(steps));
            _searcher = searcher ?? throw new ArgumentNullException(nameof(searcher));
            _cost = cost;
            _canReachForPlanning = canReachForPlanning ?? throw new ArgumentNullException(nameof(canReachForPlanning));
            _baseState = baseState;
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            _settings = settings;
            _now = now ?? throw new ArgumentNullException(nameof(now));
            _log = log;
            _planningIsClear = planningIsClear;
            _clearancePolicy = clearancePolicy ?? ClearancePolicy.Default;
            _budget = budget;
        }

        // 겹침을 감수하지 않는 규칙이면 "없었다"가 장애물 때문일 수 있음을 실패 이유에 덧붙인다.
        private string RequireClearNote => _clearancePolicy.RequireClear ? " — 장애물과 겹치지 않는 방향만 허용 중" : string.Empty;

        public IEnumerator ApproachAndSearch(ReachPlan plan, TransferResult result, Action<OrientationSearchResult> done)
        {
            var placement = new StandPlacementResult();
            yield return PlanStands(plan, placement);

            if (placement.HasOptions)
            {
                yield return StandAtPlannedOptions(plan, placement, result, done);
            }
            else
            {
                yield return StandAtEachOffset(plan, result, done);
            }
        }

        // 이동하기 전에 서 볼 자리마다 팔이 닿는 방향이 있는지 가늠한다. 가늠할 수 없는 상황이면 아무것도 하지 않아 결과가 빈 채로 남는다.
        private IEnumerator PlanStands(ReachPlan plan, StandPlacementResult placement)
        {
            if (!CanPlanInAdvance(plan, out Pose armBase, out float rootYawDeg))
            {
                yield break;
            }

            IReadOnlyList<StandCandidate> candidates = StandCandidates.Generate(
                plan.StandoffOffsets, plan.LateralOffsets, (extra, lateral) => plan.StandFor(extra, lateral).GetValueOrDefault());
            var probe = new StandReachProbe(_canReachForPlanning, armBase, rootYawDeg, plan.FacingYawDeg.Value, _planningIsClear);
            var searcher = new StandPlacementSearcher(probe.At, _cost, probe.ClearAt, _clearancePolicy, _budget);

            float startedAt = _now();
            _log?.Invoke($"{plan.ReachStep} — 서 볼 자리 {candidates.Count}곳에서 팔이 닿는 방향을 미리 가늠한다(시간이 걸린다. 그동안 로봇은 제자리에 있다).");

            yield return searcher.Search(candidates, plan.Nominal, plan.Positions, plan.Search, placement);

            int risky = 0;
            foreach (StandPlacementOption option in placement.Options)
            {
                if (option.HasCollisionRisk)
                {
                    risky++;
                }
            }

            string riskNote = risky > 0 ? $", 그중 장애물과 겹칠 수 있는 자리 {risky}곳" : string.Empty;
            _log?.Invoke(
                $"{plan.ReachStep} — 가늠 끝: 닿는 자리 {placement.Options.Count}곳{riskNote}({placement.EvaluatedCount}곳 확인, {_now() - startedAt:0.0}초).");
        }

        // 베이스를 옮길 수 있고, 서는 방향과 팔 베이스의 지금 상태를 알고, 견줄 자리가 둘 이상일 때만 미리 가늠한다.
        private bool CanPlanInAdvance(ReachPlan plan, out Pose armBase, out float rootYawDeg)
        {
            armBase = default;
            rootYawDeg = 0f;

            return _settings.PlanInAdvance
                && plan.StandFor != null
                && plan.FacingYawDeg.HasValue
                && plan.StandoffOffsets.Count * plan.LateralOffsets.Count > 1
                && _baseState != null
                && _baseState.TryGetState(out armBase, out rootYawDeg);
        }

        // 미리 고른 자리를 점수가 좋은 순서로 실제로 서 본다. 가늠은 실제와 다를 수 있어 서서 다시 찾은 결과가 최종 판정이다.
        private IEnumerator StandAtPlannedOptions(
            ReachPlan plan, StandPlacementResult placement, TransferResult result, Action<OrientationSearchResult> done)
        {
            int attempts = Mathf.Min(_settings.MaxStandAttempts, placement.Options.Count);
            for (int index = 0; index < attempts; index++)
            {
                StandCandidate candidate = placement.Options[index].Candidate;
                var attempt = new StandAttempt();
                yield return StandAndSearch(plan, candidate.StandoffExtraM, candidate.LateralM, result, attempt);

                if (attempt.IsBaseStuck)
                {
                    done(new OrientationSearchResult());
                    yield break;
                }

                if (attempt.IsFound)
                {
                    LogChosenStand(plan, attempt.Search, candidate.ShiftM);
                    done(attempt.Search);
                    yield break;
                }

                FailStand(
                    result,
                    $"{plan.ReachStep}: 미리 고른 자리(뒤로 {candidate.StandoffExtraM:0.00}m, 옆으로 {candidate.LateralM:0.00}m)에서 " +
                    $"팔이 닿는 방향을 {attempt.Search.TriedCount}가지({attempt.Search.DescribeVerdicts()}) 찾아봤지만 없었다{RequireClearNote} " +
                    $"({_diagnostics.Reach(plan.Positions, plan.Nominal)})");
            }

            if (attempts > 1)
            {
                result.Fail(TransferFailure.MotionFailed, $"{result.Detail} (미리 고른 서는 자리를 {attempts}곳 시도함)");
            }

            done(new OrientationSearchResult());
        }

        // 미리 가늠할 수 없을 때의 방식: 기본 자리에서 시작해 서는 거리를 계획의 보정값만큼 차례로 늘려 가며 서 본다.
        private IEnumerator StandAtEachOffset(ReachPlan plan, TransferResult result, Action<OrientationSearchResult> done)
        {
            foreach (float extra in plan.StandoffOffsets)
            {
                var attempt = new StandAttempt();
                yield return StandAndSearch(plan, extra, 0f, result, attempt);

                if (attempt.IsBaseStuck)
                {
                    done(new OrientationSearchResult());
                    yield break;
                }

                if (attempt.IsFound)
                {
                    LogChosenStand(plan, attempt.Search, Mathf.Abs(extra));
                    done(attempt.Search);
                    yield break;
                }

                FailStand(
                    result,
                    $"{plan.ReachStep}: 서는 자리(뒤로 {extra:0.00}m)에서 팔이 닿는 방향을 {attempt.Search.TriedCount}가지({attempt.Search.DescribeVerdicts()}) 찾아봤지만 없었다{RequireClearNote} " +
                    $"({_diagnostics.Reach(plan.Positions, plan.Nominal)})");
            }

            if (plan.StandoffOffsets.Count > 1)
            {
                result.Fail(TransferFailure.MotionFailed, $"{result.Detail} (서는 거리를 {plan.StandoffOffsets.Count}번 바꿔 시도함)");
            }

            done(new OrientationSearchResult());
        }

        // 서 본 자리에서 못 찾은 이유를 결과에 남기고 바로 알린다. 결과에는 마지막 시도의 이유만 남으므로, 앞선 자리의 이유는 이 로그로만 볼 수 있다.
        private void FailStand(TransferResult result, string detail)
        {
            result.Fail(TransferFailure.MotionFailed, detail);
            _log?.Invoke(detail);
        }

        // 경유지(있으면)를 거쳐 서는 자리로 가서, 거기서 닿는 도구 방향을 실제 팔로 찾는다. 결과는 attempt에 남긴다.
        private IEnumerator StandAndSearch(ReachPlan plan, float extra, float lateral, TransferResult result, StandAttempt attempt)
        {
            bool isOk = false;

            if (plan.WaypointFor != null)
            {
                yield return _steps.MoveBase(plan.WaypointFor(extra, lateral), plan.FacingYawDeg, plan.WaypointStep, result, ok => isOk = ok);
                if (!isOk)
                {
                    attempt.MarkBaseStuck();
                    yield break;
                }
            }

            Vector3? stand = plan.StandFor != null ? plan.StandFor(extra, lateral) : null;
            yield return _steps.MoveBase(stand, plan.FacingYawDeg, plan.StandStep, result, ok => isOk = ok);
            if (!isOk)
            {
                attempt.MarkBaseStuck();
                yield break;
            }

            var search = new OrientationSearchResult();
            yield return _searcher.Search(plan.Nominal, plan.Positions, plan.Search, search);
            attempt.RecordSearch(search);
        }

        // 기본 자리나 기본 방향이 아닌 곳을 골랐을 때, 또는 장애물과 겹칠 수 있는 방향을 감수했을 때 알린다.
        private void LogChosenStand(ReachPlan plan, OrientationSearchResult search, float shiftM)
        {
            if (search.HasCollisionRisk)
            {
                _log?.Invoke(
                    $"{plan.ReachStep} — 경고: 장애물과 겹치지 않는 도구 방향을 찾지 못해, 닿는 방향 중 가장 덜 무리한 방향으로 간다. " +
                    "팔이 선반이나 박스에 닿을 수 있다(링크 상자 기준이라 실제로는 닿지 않을 수도 있다).");
            }

            if (shiftM <= 0f && search.DeviationDeg <= 0f)
            {
                return;
            }

            _log?.Invoke(
                $"{plan.ReachStep} — 서는 자리를 기본에서 {shiftM:0.00}m 옮기고, 도구 방향을 기본에서 " +
                $"기울이기 {search.PitchDeg:0}°, 좌우 {search.YawDeg:0}°, 손목 비틀기 {search.RollDeg:0}°로 바꾼다({search.TriedCount}번째 후보).");
        }
    }
}
