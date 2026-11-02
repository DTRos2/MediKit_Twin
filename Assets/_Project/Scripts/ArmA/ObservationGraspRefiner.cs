using System;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물건 앞에서 카메라로 물건을 찾아보고, 쥐는 자리가 본 물건 위치에서 벗어났으면 접근·집기·후퇴 자리를 같은 만큼 옮겨 고친다.
    ///  - 물건을 찾지 못하면: 인식이 필수(RequireRecognition)이면 집지 않고 멈추고, 아니면 경고를 남기고 계획한 자리 그대로 집는다.
    ///  - 기준은 계획한 쥐는 자리(도구 끝점)이고, 쥐는 자리는 물건 중심에 맞추는 것이 목표다(그리퍼는 도구 끝점 반경 안의 물건을 쥔다).
    ///    다만 손가락 끝이 도구 끝점보다 아래로 내려오므로, 얇은 물건은 중심보다 조금 높게 겨냥해 손가락이 선반 판에 눌리지 않게 한다(GraspLift).
    ///    스스로 찾은 자세는 쥐는 자리가 요청 시점의 물건 위치이므로 물건이 그 사이 움직인 만큼을 고치고,
    ///    알려 준 자세는 알려 줄 때와 물건 자리가 달라진 만큼까지 고친다. 본 위치는 인식기가 내놓은 지금 값이다.
    ///    (예전에는 요청 시점의 물건 위치와 비교했는데, 알려 준 자세에서는 둘이 항상 같아서 쥐는 자리가 물건에서 멀어도 고치지 않았다.)
    ///  - 옮긴 양은 한계(MaxCorrectionM)까지만, 잡음 수준(MinCorrectionM)보다 작으면 옮기지 않는다(GraspCorrection).
    ///  - 관절 각도 길로 가는 중이면 옮긴 자리의 길을 쥐는 자리의 관절 각도에서 출발해 다시 푼다. 못 풀면 고치지 않는다.
    ///  - 옮긴 자리가 장애물과 겹치는데 계획한 자리는 겹치지 않았다면, 옮기는 양을 줄여 가며(ShiftBackoff) 겹치지 않는 만큼만 옮긴다.
    ///    줄여도 겹치거나 관절 각도를 못 풀면 고치지 않는다.
    /// 인식기, 길을 푸는 쪽, 겹침 판정은 모두 주입받으므로 씬 없이 시험한다.
    /// </summary>
    public sealed class ObservationGraspRefiner : IGraspRefiner
    {
        private readonly IItemPoseEstimator _estimator;
        private readonly RecognitionSettings _settings;
        private readonly SeededRouteBuilder _routes;
        private readonly Func<GraspTargets, bool> _isClear;
        private readonly Action<string> _log;
        private readonly ShiftBackoff _backoff;

        /// <param name="routes">고친 자리의 관절 각도 길을 푸는 쪽. 관절 각도 길로 가지 않으면 null이어도 된다.</param>
        /// <param name="isClear">세 자리가 장애물과 겹치지 않으면 true. 겹침을 따지지 않으면 null.</param>
        /// <param name="log">진행을 알리는 쪽. 필요 없으면 null.</param>
        /// <param name="backoff">고칠 양을 그대로 못 옮길 때(장애물에 닿거나 관절 각도를 못 풀 때) 줄여 보는 방식. null이면 기본(전부, 3/4, 1/2, 1/4)이다.</param>
        public ObservationGraspRefiner(
            IItemPoseEstimator estimator,
            RecognitionSettings settings,
            SeededRouteBuilder routes,
            Func<GraspTargets, bool> isClear,
            Action<string> log,
            ShiftBackoff backoff = null)
        {
            _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _routes = routes;
            _isClear = isClear;
            _log = log;
            _backoff = backoff ?? ShiftBackoff.Default;
        }

        public GraspRefinement Refine(ItemPickRequest request, GraspTargets planned)
        {
            string slotId = request.Item.SlotId;
            ItemObservation seen = _estimator.Observe(request.Item);

            if (!seen.IsDetected)
            {
                return OnNotDetected(slotId, seen, planned);
            }

            // 손가락 끝이 도구 끝점보다 아래로 내려오므로, 얇은 물건은 중심보다 높게 겨냥해야 판에 눌리지 않는다.
            float lift = GraspLift.Compute(_settings.FingerDropBelowToolM, ItemExtent.HalfHeightOf(request.Item.Prototype));
            string liftNote = lift > 0f ? $", 손가락이 판에 닿지 않게 물건 중심보다 {lift * 100f:0.0}cm 높게 겨냥" : string.Empty;
            string seenText = $"칸 {slotId}: 카메라로 물건을 확인했다(거리 {seen.DistanceM:0.00}m{liftNote})";
            Vector3 aim = seen.EstimatedPose.position + Vector3.up * lift;
            Vector3 shift = GraspCorrection.Compute(planned.Grasp, aim, _settings.MinCorrectionM, _settings.MaxCorrectionM, out bool isClamped);
            if (shift == Vector3.zero)
            {
                return Keep(planned, $"{seenText} — 쥐는 자리가 물건 위치와 거의 같아 자리를 고치지 않는다.");
            }

            if (!TryShift(planned, shift, out GraspTargets shifted, out float fraction, out string whyNot))
            {
                return Keep(planned, $"{seenText} — 쥐는 자리가 물건에서 {shift.magnitude * 100f:0.0}cm 벗어났지만 {whyNot} 계획한 자리 그대로 집는다.");
            }

            string clampNote = isClamped ? $"(한 번에 고치는 한계 {_settings.MaxCorrectionM * 100f:0.0}cm까지만)" : string.Empty;
            string backoffNote = fraction < 1f ? $" 다만 {whyNot} {fraction * 100f:0}%만 옮긴다" : string.Empty;
            string note = $"{seenText} — 쥐는 자리가 물건에서 ({-shift.x * 100f:+0.0;-0.0}, {-shift.y * 100f:+0.0;-0.0}, {-shift.z * 100f:+0.0;-0.0})cm 벗어나 물건 쪽으로 고친다{clampNote}{backoffNote}. 쥐는 자리 높이 {planned.Grasp.y:0.000}→{shifted.Grasp.y:0.000}m(물건 중심 {seen.EstimatedPose.position.y:0.000}m).";
            _log?.Invoke(note);
            return GraspRefinement.Corrected(shifted, note);
        }

        private GraspRefinement OnNotDetected(string slotId, ItemObservation seen, GraspTargets planned)
        {
            string note = $"칸 {slotId}: 카메라로 물건을 확인하지 못했다 — {seen.Describe()}.";
            if (_settings.RequireRecognition)
            {
                _log?.Invoke($"{note} 인식이 필수라 집지 않는다.");
                return GraspRefinement.Rejected(planned, note);
            }

            return Keep(planned, $"{note} 계획한 자리 그대로 집는다.");
        }

        private GraspRefinement Keep(GraspTargets planned, string note)
        {
            _log?.Invoke(note);
            return GraspRefinement.Keep(planned, note);
        }

        // 전부 옮기는 것부터 시작해 안 되면 양을 줄여 가며, 되는 첫 양으로 옮긴다. 줄여도 안 되면 이유와 함께 false.
        // whyNot에는 전부 옮기지 못한 이유를 남긴다(줄여서 옮겼다면 그 이유를 알려 주는 데 쓴다).
        // 선반 바닥판처럼 물건 바로 밑에 있는 장애물 때문에 전부는 못 내려가도, 쥘 수 있는 거리 안까지는 내려갈 수 있기 때문이다.
        private bool TryShift(GraspTargets planned, Vector3 shift, out GraspTargets shifted, out float fraction, out string whyNot)
        {
            whyNot = string.Empty;
            shifted = planned;
            fraction = 0f;

            // 계획한 자리가 이미 겹치면 옮겨서 더 나빠지는 것이 아니므로 겹침은 따지지 않는다.
            bool isPlannedClear = _isClear == null || _isClear(planned);

            for (int i = 0; i < _backoff.Fractions.Count; i++)
            {
                float candidateFraction = _backoff.Fractions[i];
                string why = TryBuild(planned, shift * candidateFraction, isPlannedClear, out GraspTargets candidate);
                if (why.Length == 0)
                {
                    shifted = candidate;
                    fraction = candidateFraction;
                    return true;
                }

                if (i == 0)
                {
                    whyNot = why;
                }
            }

            return false;
        }

        // 자리를 옮기고, 필요하면 관절 길을 다시 풀고, 겹침을 확인한다. 되면 빈 글자, 안 되면 이유.
        private string TryBuild(GraspTargets planned, Vector3 shift, bool isPlannedClear, out GraspTargets shifted)
        {
            shifted = planned.ShiftedBy(shift, planned.Route);

            if (planned.Route != null)
            {
                if (_routes == null
                    || !_routes.TryBuild(planned.Route.Grasp, shifted.NearPose, shifted.GraspPose, shifted.RetreatPose, out TaughtJointRoute route))
                {
                    return "옮긴 자리의 관절 각도를 풀지 못해";
                }

                shifted = planned.ShiftedBy(shift, route);
            }

            if (_isClear != null && isPlannedClear && !_isClear(shifted))
            {
                return "옮기면 팔이 장애물과 겹쳐";
            }

            return string.Empty;
        }
    }
}
