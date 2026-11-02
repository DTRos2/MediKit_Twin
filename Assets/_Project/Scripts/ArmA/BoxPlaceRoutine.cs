using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 쥔 물건을 박스 슬롯에 놓는 순서: 박스 곁에 서서 닿는 도구 방향을 찾고(StandSearchRoutine), 슬롯 위 → 슬롯 → 놓기 → 들어올리기.
    /// 물건을 쥔 채 지그 쪽으로 돌아가 선다. 팔은 관절 각도를 유지하므로 베이스와 함께 움직인다.
    /// 슬롯 위에서 내려가 놓는다. 쥐고 놓는 방식은 IItemHolder, 움직임 한 단계는 ArmMotionSteps가 맡는다.
    ///
    /// 물건을 물리로 내려앉히면 도구가 물건을 쥔 자세 그대로 물건이 떨어지므로, 도구 방향이 곧 물건이 놓이는 방향이다.
    /// 그래서 쥔 물건이 도구에 대해 어떤 자세인지 재서(HeldItemFrame), 물건이 선반에 놓여 있던 방향 그대로 놓이는 도구 방향과 자리를 먼저 찾는다.
    /// 선반에서는 도구를 수평으로 들여 집지만 박스에는 위에서 내려놓으면 물건이 90° 돌아 모서리로 서기 때문이다.
    /// 그 방향으로 닿지 못하면 예전처럼 똑바로 아래를 보고 놓는다. 쥔 자세를 알 수 없는 방식(스크립트 부착)도 예전대로 놓는다.
    ///
    /// 놓는 높이는 낮은 후보부터 시도해 팔이 닿는 가장 낮은 높이로 정한다(ReleaseHeights). 좁은 박스에서는 끝 슬롯처럼 손가락이 벽 위에
    /// 머물러야만 닿는 곳이 있지만, 가운데 슬롯은 낮게 들어가 물건을 덜 떨어뜨릴 수 있기 때문이다.
    /// </summary>
    public sealed class BoxPlaceRoutine
    {
        // 서는 자리를 고쳤다는 로그 앞에 붙는 이름. 선반 쪽 서는 자리와 구별한다.
        private const string BaseClearanceLabel = "박스";

        // 기본 서는 자리만 보는 계획에 넘기는 값: 기본 거리에서 더 물러나지도, 옆으로 비키지도 않는다.
        private static readonly IReadOnlyList<float> DefaultStandOnly = new[] { 0f };

        private readonly StandPolicy _policy;
        private readonly StandSearchRoutine _standSearch;
        private readonly ArmMotionSteps _steps;
        private readonly IItemHolder _holder;
        private readonly IHeldItemSource _heldSource;
        private readonly BoxPlaceSettings _settings;
        private readonly Func<Pose?> _toolPose;
        private readonly Action<string> _log;
        private readonly BaseStandClearance _baseClearance;
        private readonly BoxPlaceDiagnostics _diagnostics;
        private readonly IArmPhaseSource _phases;

        /// <param name="toolPose">도구 끝점의 지금 월드 자세. 팔이 준비되지 않았으면 null을 돌려준다. 없으면 쥔 자세를 재지 못한다.</param>
        /// <param name="log">진행을 알리는 쪽. 필요 없으면 null.</param>
        /// <param name="baseClearance">서는 자리에서 팔 베이스가 박스 옆 레일 같은 장애물에 파고들면 뒤로 물려 주는 쪽. 없으면 정해진 자리에 그대로 선다.</param>
        /// <param name="diagnostics">가늠한 것과 실제로 하는 것의 차이, 물건의 미끄러짐을 로그로 남기는 쪽. 없으면 남기지 않는다.</param>
        /// <param name="phases">팔 베이스를 옮기는 동안 접은 뒤, 도착한 뒤를 알려 주는 쪽. 있으면 그 지점에서도 미끄러짐을 잰다. 없으면 재지 않는다.</param>
        public BoxPlaceRoutine(
            StandPolicy policy, StandSearchRoutine standSearch, ArmMotionSteps steps, IItemHolder holder, BoxPlaceSettings settings,
            Func<Pose?> toolPose = null, Action<string> log = null, BaseStandClearance baseClearance = null,
            BoxPlaceDiagnostics diagnostics = null, IArmPhaseSource phases = null)
        {
            _baseClearance = baseClearance;
            _diagnostics = diagnostics;
            _phases = phases;
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _standSearch = standSearch ?? throw new ArgumentNullException(nameof(standSearch));
            _steps = steps ?? throw new ArgumentNullException(nameof(steps));
            _holder = holder ?? throw new ArgumentNullException(nameof(holder));
            _heldSource = holder as IHeldItemSource;
            _settings = settings;
            _toolPose = toolPose;
            _log = log;
        }

        public IEnumerator Run(ItemPlaceRequest request, TransferResult result)
        {
            HeldItemFrame? frame = TryMeasureHeldItem(out HeldItemFrame measured, out Quaternion restingRotation) ? measured : (HeldItemFrame?)null;

            if (frame.HasValue && _diagnostics != null)
            {
                _log?.Invoke($"박스 진단: {_diagnostics.DescribeHeldFrame(frame.Value)}");
            }

            // 지금(들어 올린 직후)의 쥔 자세를 기준으로, 박스 곁·슬롯 위·놓기 직전에 재서 어느 구간에서 미끄러지는지 가른다.
            HeldSlipTrail slipTrail = frame.HasValue && _diagnostics != null ? new HeldSlipTrail(frame.Value) : null;

            // 베이스를 옮기는 동안의 접은 뒤, 도착한 뒤에도 잰다. 놓는 순서가 끝나면(실패해도) 반드시 알림을 끊어야 다음 물건에 섞이지 않는다.
            Action<ArmMotionPhase> onPhase = SubscribeToArmPhases(slipTrail);
            try
            {
                yield return PlaceWithTrail(request, result, frame, restingRotation, slipTrail);
            }
            finally
            {
                if (onPhase != null)
                {
                    _phases.PhaseReached -= onPhase;
                }
            }
        }

        // 서는 자리를 찾고 슬롯 위 → 슬롯 → 놓기 → 들어올리기를 한다. 팔 베이스를 옮기는 동안의 알림은 Run이 받아 둔다.
        private IEnumerator PlaceWithTrail(
            ItemPlaceRequest request, TransferResult result, HeldItemFrame? frame, Quaternion restingRotation, HeldSlipTrail slipTrail)
        {
            var search = new OrientationSearchResult();
            ReleaseHeights heights = _settings.ReleaseHeights;
            float releaseHeight = 0f;
            // 방향을 찾을 때 도구 자리를 계산한 기준 방향. 찾은 방향으로 다시 계산한 자리와 견주려고 남긴다.
            Quaternion searchedNominal = Quaternion.identity;

            if (frame.HasValue && _settings.PlacesInRestingOrientation)
            {
                // 물건이 선반에 놓여 있던 방향 그대로 놓이는 도구 방향. 이 방향으로 닿지 못해도 아직 실패가 아니므로 결과는 따로 받는다.
                Quaternion restingTool = frame.Value.ToolRotationFor(restingRotation);
                Vector3 toolForward = restingTool * Vector3.forward;
                Vector3 toolUp = restingTool * Vector3.up;
                _log?.Invoke(
                    "박스: 물건을 선반에 놓여 있던 방향 그대로 놓으려면 도구가 " +
                    $"앞 ({toolForward.x:0.00}, {toolForward.y:0.00}, {toolForward.z:0.00}), 위 ({toolUp.x:0.00}, {toolUp.y:0.00}, {toolUp.z:0.00})를 봐야 한다.");

                var lastAttempt = new TransferResult();
                // 낮은 높이부터 시도해 닿는 가장 낮은 높이에서 놓는다. 물건이 덜 떨어지고, 박스 안에서 놓을수록 슬롯 자리에 가깝게 놓인다.
                // 서 볼 자리를 여러 곳 훑지 않고 기본 자리 한 곳에서만 찾는다. 여러 곳을 미리 가늠하면 한 번에 10초 가까이 팔이 멈춰 서 있고,
                // 이 방향은 닿는 자리가 좁아 다른 자리로 옮겨도 닿는 경우가 드물다. 못 찾으면 곧바로 위에서 내려놓는 쪽이 전체 시간을 아낀다.
                for (int index = 0; index < heights.Count && !search.IsFound; index++)
                {
                    var attempt = new TransferResult();
                    yield return _standSearch.ApproachAndSearch(
                        PlanFor(request, frame, restingTool, _settings.RestingSearch, DefaultStandOnly, DefaultStandOnly, heights[index]),
                        attempt, found => search = found);

                    if (search.IsFound)
                    {
                        releaseHeight = heights[index];
                        searchedNominal = restingTool;
                    }
                    else
                    {
                        lastAttempt = attempt;
                    }
                }

                if (!search.IsFound)
                {
                    _log?.Invoke(
                        "박스: 물건을 선반에서 놓여 있던 방향 그대로 놓을 도구 방향을 찾지 못해 위에서 아래로 내려놓는다(물건이 모서리로 설 수 있다). " +
                        $"(놓는 높이 {heights.Count}가지를 모두 시도함) {lastAttempt.Detail}");
                }
            }

            if (!search.IsFound)
            {
                var lastAttempt = new TransferResult();
                // 낮은 높이부터 시도한다. 서는 자리까지 바꿔 가며 찾으므로, 낮은 높이가 안 될수록 시간이 더 든다.
                for (int index = 0; index < heights.Count && !search.IsFound; index++)
                {
                    var attempt = new TransferResult();
                    yield return _standSearch.ApproachAndSearch(
                        PlanFor(
                            request, frame, _settings.NominalToolRotation(), _settings.Search,
                            _policy.PlaceStandoffOffsets, _policy.LateralOffsets, heights[index]),
                        attempt, found => search = found);

                    if (search.IsFound)
                    {
                        releaseHeight = heights[index];
                        searchedNominal = _settings.NominalToolRotation();
                    }
                    else
                    {
                        lastAttempt = attempt;
                    }
                }

                if (!search.IsFound)
                {
                    // 마지막으로 시도한(가장 높은) 높이의 실패 이유를 호출한 쪽에 남긴다.
                    result.Fail(lastAttempt.Failure, lastAttempt.Detail);
                    yield break;
                }
            }

            // 서는 자리까지 베이스와 팔이 움직인 뒤다. 들어 올린 직후와 견주면 그 이동 중에 미끄러졌는지 안다.
            MarkSlip(slipTrail, SlipPoints.BoxSide);

            if (releaseHeight > 0f)
            {
                _log?.Invoke($"박스: 슬롯 중심보다 {releaseHeight * 100f:0}cm 위에서 놓는다(닿는 가장 낮은 높이).");
            }

            Quaternion rotation = search.Rotation;
            PlacePath path = PlacePath.For(
                frame, request.WorldPose.position, rotation, _settings.ApproachHeightM, _settings.LiftHeightM, releaseHeight);
            LogPlan(frame, request, searchedNominal, rotation, releaseHeight, path);

            bool isOk = false;
            yield return _steps.MoveTool(new Pose(path.Above, rotation), TransferSteps.BoxAbove, result, ok => isOk = ok);
            if (!isOk)
            {
                yield break;
            }

            MarkSlip(slipTrail, SlipPoints.AboveSlot);

            yield return _steps.MoveToolPrecisely(new Pose(path.Slot, rotation), TransferSteps.BoxSlot, result, ok => isOk = ok);
            if (!isOk)
            {
                yield break;
            }

            LogBeforeRelease(slipTrail, request, releaseHeight, path);
            yield return _holder.Release(request);

            yield return _steps.MoveTool(new Pose(path.Lift, rotation), TransferSteps.BoxLift, result, ok => isOk = ok);
            if (!isOk)
            {
                yield break;
            }

            result.Succeed();
        }

        // 방향을 찾을 때 시험한 도구 자리(기본 방향 기준)와, 찾은 방향으로 다시 계산해 실제로 가는 도구 자리가 다른지 남긴다.
        private void LogPlan(
            HeldItemFrame? frame, ItemPlaceRequest request, Quaternion searchedNominal, Quaternion rotation, float releaseHeight, PlacePath executed)
        {
            if (_diagnostics == null || _log == null || !frame.HasValue)
            {
                return;
            }

            PlacePath tested = PlacePath.For(
                frame, request.WorldPose.position, searchedNominal, _settings.ApproachHeightM, _settings.LiftHeightM, releaseHeight);
            _log($"박스 진단: {_diagnostics.DescribePlan(tested, executed, rotation)}");
        }

        // 팔 베이스를 옮기는 단계(접은 뒤, 도착한 뒤)마다 미끄러짐을 잰다. 같은 단계가 되풀이되면 몇 번째인지 이름에 붙인다.
        // 알림을 받지 않으면(진단을 껐거나 알려 주는 쪽이 없음) null을 돌려준다.
        private Action<ArmMotionPhase> SubscribeToArmPhases(HeldSlipTrail trail)
        {
            if (trail == null || _phases == null)
            {
                return null;
            }

            var counts = new Dictionary<ArmMotionPhase, int>();
            Action<ArmMotionPhase> onPhase = phase =>
            {
                counts.TryGetValue(phase, out int seen);
                counts[phase] = ++seen;
                MarkSlip(trail, SlipPoints.Label(SlipPoints.For(phase), seen));
            };

            _phases.PhaseReached += onPhase;
            return onPhase;
        }

        // 한 지점에서 쥔 자세를 재서 직전 지점·처음 지점과 견준 미끄러짐을 남긴다. 재지 못하면(진단을 껐거나 쥔 자세를 알 수 없음) 아무것도 하지 않는다.
        private void MarkSlip(HeldSlipTrail trail, string point)
        {
            if (trail == null || _log == null || !TryMeasureHeldItem(out HeldItemFrame now, out _))
            {
                return;
            }

            _log($"박스 진단: {_diagnostics.DescribeSlipMark(point, trail.Mark(now))}");
        }

        // 놓기 직전에 미끄러짐을 마지막으로 재고, 물건과 도구가 목표에서 얼마나 벗어났는지 남긴다.
        private void LogBeforeRelease(HeldSlipTrail trail, ItemPlaceRequest request, float releaseHeight, PlacePath path)
        {
            if (trail == null || _log == null)
            {
                return;
            }

            MarkSlip(trail, SlipPoints.BeforeRelease);

            Pose? tool = _toolPose();
            if (!tool.HasValue || !_heldSource.TryGetHeldItem(out HeldItem held))
            {
                return;
            }

            Vector3 expectedCenter = request.WorldPose.position + Vector3.up * releaseHeight;
            _log($"박스 진단: 놓기 직전 — {_diagnostics.DescribeRelease(expectedCenter, held.Center, path.Slot, tool.Value.position)}");
        }

        // 쥔 물건이 도구에 대해 어떤 자세인지 잰다. 쥔 자세를 알려 주는 방식이 아니거나 도구 자세를 읽을 수 없으면 false.
        private bool TryMeasureHeldItem(out HeldItemFrame frame, out Quaternion restingRotation)
        {
            frame = default;
            restingRotation = Quaternion.identity;

            if (_heldSource == null || _toolPose == null)
            {
                return false;
            }

            Pose? tool = _toolPose();
            if (!tool.HasValue || !_heldSource.TryGetHeldItem(out HeldItem held))
            {
                return false;
            }

            frame = HeldItemFrame.Measure(tool.Value, held.Center, held.Rotation);
            restingRotation = held.RestingRotation;
            return true;
        }

        // 도구가 nominal 방향일 때 물건 중심이 슬롯 위, 슬롯, 들어올린 자리를 지나도록 도구 자리를 정해 서는 자리와 방향 찾기 계획을 만든다.
        private ReachPlan PlanFor(
            ItemPlaceRequest request, HeldItemFrame? frame, Quaternion nominal, OrientationSearchSettings search,
            IReadOnlyList<float> standoffOffsets, IReadOnlyList<float> lateralOffsets, float releaseHeightM)
        {
            Vector3 slotPosition = request.WorldPose.position;
            PlacePath path = PlacePath.For(frame, slotPosition, nominal, _settings.ApproachHeightM, _settings.LiftHeightM, releaseHeightM);

            // 끝 슬롯처럼 박스 곁 레일에 가까운 자리는 "슬롯에서 정해진 거리"만으로 정하면 베이스가 레일에 파고든다.
            Func<float, float, Vector3?> standFor = (extra, lateral) => _policy.BoxStand(slotPosition, extra, lateral);
            float? facingYawDeg = _policy.BoxFacingYawDeg();
            if (_baseClearance != null)
            {
                standFor = _baseClearance.Apply(standFor, facingYawDeg, BaseClearanceLabel);
            }

            return new ReachPlan(
                standoffOffsets,
                lateralOffsets,
                null,
                standFor,
                nominal,
                path.Positions,
                search,
                string.Empty,
                TransferSteps.BoxStandPoint,
                TransferSteps.BoxAbove,
                facingYawDeg);
        }
    }
}
