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
    public sealed class ObservationGraspRefiner : IGraspRefiner, IGraspSettler
    {
        private readonly IItemPoseEstimator _estimator;
        private readonly RecognitionSettings _settings;
        private readonly SeededRouteBuilder _routes;
        private readonly Func<GraspTargets, bool> _isClear;
        private readonly Action<string> _log;
        private readonly ShiftBackoff _backoff;
        private readonly Func<Pose> _toolPose;

        /// <param name="routes">고친 자리의 관절 각도 길을 푸는 쪽. 관절 각도 길로 가지 않으면 null이어도 된다.</param>
        /// <param name="isClear">세 자리가 장애물과 겹치지 않으면 true. 겹침을 따지지 않으면 null.</param>
        /// <param name="log">진행을 알리는 쪽. 필요 없으면 null.</param>
        /// <param name="backoff">고칠 양을 그대로 못 옮길 때(장애물에 닿거나 관절 각도를 못 풀 때) 줄여 보는 방식. null이면 기본(전부, 3/4, 1/2, 1/4)이다.</param>
        /// <param name="toolPose">지금 도구 끝점 자세(월드). 주면 쥐는 자리에 닿은 뒤 실제 오차를 메운다(IGraspSettler). 없으면 메우지 않는다.</param>
        public ObservationGraspRefiner(
            IItemPoseEstimator estimator,
            RecognitionSettings settings,
            SeededRouteBuilder routes,
            Func<GraspTargets, bool> isClear,
            Action<string> log,
            ShiftBackoff backoff = null,
            Func<Pose> toolPose = null)
        {
            _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _routes = routes;
            _isClear = isClear;
            _log = log;
            _backoff = backoff ?? ShiftBackoff.Default;
            _toolPose = toolPose;
        }

        public int SettleAttempts => _toolPose != null ? _settings.SettleAttempts : 0;

        public float SettleWaitSeconds => _settings.SettleWaitSeconds;

        public GraspRefinement Refine(ItemPickRequest request, GraspTargets planned)
        {
            string slotId = request.Item.SlotId;
            ItemObservation seen = _estimator.Observe(request.Item);

            if (!seen.IsDetected)
            {
                return OnNotDetected(slotId, seen, planned);
            }

            // 손가락 끝이 도구 끝점보다 아래로 내려오므로, 얇은 물건은 중심보다 높게 겨냥해야 판에 눌리지 않는다.
            float lift = LiftFor(request);
            string liftNote = lift > 0f ? $", 손가락이 판에 닿지 않게 물건 중심보다 {lift * 100f:0.0}cm 높게 겨냥" : string.Empty;
            string seenText = $"칸 {slotId}: 카메라로 물건을 확인했다(거리 {seen.DistanceM:0.00}m{liftNote})";
            Vector3 aim = AimAt(seen.EstimatedPose.position, lift, planned.Rotation);
            Vector3 shift = GraspCorrection.Compute(planned.Grasp, aim, _settings.MinCorrectionM, _settings.MaxCorrectionM, out bool isClamped);
            float tiltDeg = _settings.GraspTiltDeg;
            if (shift == Vector3.zero && tiltDeg <= 0f)
            {
                return Keep(planned, $"{seenText} — 쥐는 자리가 물건 위치와 거의 같아 자리를 고치지 않는다.");
            }

            if (!TryShift(planned, shift, tiltDeg, out GraspTargets shifted, out float fraction, out float usedTiltDeg, out string whyNot))
            {
                return Keep(planned, $"{seenText} — 쥐는 자리가 물건에서 {shift.magnitude * 100f:0.0}cm 벗어났지만 {whyNot} 계획한 자리 그대로 집는다.");
            }

            bool tiltDropped = tiltDeg > 0f && usedTiltDeg <= 0f;
            string clampNote = isClamped ? $"(한 번에 고치는 한계 {_settings.MaxCorrectionM * 100f:0.0}cm까지만)" : string.Empty;
            // 숙이지 못해 숙임을 뺀 경우에는 whyNot이 그 이유이므로 줄여 옮긴다는 말과 섞지 않는다.
            string backoffNote = fraction < 1f && !tiltDropped ? $" 다만 {whyNot} {fraction * 100f:0}%만 옮긴다" : string.Empty;
            string tiltNote = usedTiltDeg > 0f
                ? $" 손가락 끝을 {usedTiltDeg:0}° 숙여 쥔다."
                : tiltDropped ? $" 손가락 끝은 숙이지 않고 옮기기만 한다({whyNot})." : string.Empty;
            string note = $"{seenText} — 쥐는 자리가 물건에서 ({-shift.x * 100f:+0.0;-0.0}, {-shift.y * 100f:+0.0;-0.0}, {-shift.z * 100f:+0.0;-0.0})cm 벗어나 물건 쪽으로 고친다{clampNote}{backoffNote}. 쥐는 자리 높이 {planned.Grasp.y:0.000}→{shifted.Grasp.y:0.000}m(물건 중심 {seen.EstimatedPose.position.y:0.000}m).{tiltNote}";
            _log?.Invoke(note);
            return GraspRefinement.Corrected(shifted, note);
        }

        /// <summary>
        /// 쥐는 자리에 닿은 뒤 도구 끝점의 실제 오차를 메운다. 겨냥은 물건을 지금 다시 본 위치(와 판에 눌리지 않을 여유)이고,
        /// 새 명령은 지금 명령에 (겨냥 - 실제 도구 끝점)을 보탠 것이다. 처짐 같은 꾸준한 오차는 한 번 보태면 사라진다.
        /// 도구 방향은 이미 숙인 대로 두고(다시 숙이지 않는다), 위치만 옮겨 관절 길을 다시 푼다.
        /// </summary>
        public GraspRefinement Settle(ItemPickRequest request, GraspTargets current)
        {
            string slotId = request.Item.SlotId;
            if (_toolPose == null)
            {
                return GraspRefinement.Keep(current, string.Empty);
            }

            ItemObservation seen = _estimator.Observe(request.Item);
            if (!seen.IsDetected)
            {
                return GraspRefinement.Keep(current, $"칸 {slotId}: 쥐는 자리 마무리 — 물건을 다시 확인하지 못해 그대로 쥔다.");
            }

            Vector3 aim = AimAt(seen.EstimatedPose.position, LiftFor(request), current.Rotation);
            Vector3 actual = _toolPose().position;
            Vector3 error = GraspCorrection.Compute(actual, aim, _settings.SettleToleranceM, _settings.MaxSettleM, out bool isClamped);
            if (error == Vector3.zero)
            {
                // 흔한 경우라 로그에는 남기지 않는다(쥘 때마다 줄이 늘지 않게). 알림 글만 돌려준다.
                return GraspRefinement.Keep(current, $"칸 {slotId}: 쥐는 자리 마무리 — 도구 끝점이 겨냥과 {(aim - actual).magnitude * 1000f:0.0}mm 차이라 그대로 쥔다.");
            }

            if (!TryShift(current, error, 0f, out GraspTargets settled, out float fraction, out _, out string whyNot))
            {
                return Keep(current, $"칸 {slotId}: 쥐는 자리 마무리 — 도구 끝점이 겨냥에서 {error.magnitude * 100f:0.0}cm 모자라지만 {whyNot} 그대로 쥔다.");
            }

            string clampNote = isClamped ? $"(한 번에 메우는 한계 {_settings.MaxSettleM * 100f:0.0}cm까지만)" : string.Empty;
            string backoffNote = fraction < 1f ? $" 다만 {whyNot} {fraction * 100f:0}%만 옮긴다" : string.Empty;
            string note = $"칸 {slotId}: 쥐는 자리 마무리 — 도구 끝점이 겨냥에서 ({error.x * 100f:+0.0;-0.0}, {error.y * 100f:+0.0;-0.0}, {error.z * 100f:+0.0;-0.0})cm 모자라 그만큼 더 보내 메운다{clampNote}{backoffNote}.";
            _log?.Invoke(note);
            return GraspRefinement.Corrected(settled, note);
        }

        // 도구 끝점이 가야 할 겨냥 자리: 물건 중심에서 판에 눌리지 않을 만큼 높이고, 손가락 쌍의 가운데가 물건 가운데에 오도록
        // 손가락이 닫히는 축(도구 y축)으로 어긋난 만큼 반대쪽으로 옮긴다. 도구 끝점 기준으로 맞추면 한쪽 손가락이 먼저 물건에 닿기 때문이다.
        private Vector3 AimAt(Vector3 itemCenter, float lift, Quaternion toolRotation)
        {
            Vector3 closingAxis = toolRotation * Vector3.up;
            return itemCenter + Vector3.up * lift - closingAxis * _settings.GripCenterOffsetM;
        }

        // 물건 중심보다 얼마나 높게 겨냥할지(m). 손가락 뿌리가 판에 눌리지 않게 하는 여유다.
        private float LiftFor(ItemPickRequest request)
        {
            return GraspLift.Compute(_settings.FingerDropBelowToolM, ItemExtent.HalfHeightOf(request.Item.Prototype));
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
        // 손끝을 숙이는 것은 얇은 물건을 위한 덤이다. 숙인 자세의 관절을 풀지 못해 옮기는 것까지 포기하면 손가락이 물건에 닿지 못하므로,
        // 숙인 채로 어떤 양도 안 되면 숙이지 않고 옮기는 것을 한 번 더 해 본다. 어느 쪽으로 옮겼는지는 usedTiltDeg로 알린다.
        private bool TryShift(GraspTargets planned, Vector3 shift, float tiltDeg, out GraspTargets shifted, out float fraction, out float usedTiltDeg, out string whyNot)
        {
            whyNot = string.Empty;
            shifted = planned;
            fraction = 0f;
            usedTiltDeg = tiltDeg;

            // 계획한 자리가 이미 겹치면 옮겨서 더 나빠지는 것이 아니므로 겹침은 따지지 않는다.
            bool isPlannedClear = _isClear == null || _isClear(planned);

            bool canDropTilt = tiltDeg > 0f;
            for (int attempt = 0; attempt < (canDropTilt ? 2 : 1); attempt++)
            {
                float candidateTilt = attempt == 0 ? tiltDeg : 0f;
                for (int i = 0; i < _backoff.Fractions.Count; i++)
                {
                    float candidateFraction = _backoff.Fractions[i];
                    string why = TryBuild(planned, shift * candidateFraction, candidateTilt, isPlannedClear, out GraspTargets candidate);
                    if (why.Length == 0)
                    {
                        shifted = candidate;
                        fraction = candidateFraction;
                        usedTiltDeg = candidateTilt;
                        return true;
                    }

                    // 이유는 처음 시도(숙인 채, 전부 옮기기)가 안 된 까닭을 남긴다.
                    if (attempt == 0 && i == 0)
                    {
                        whyNot = why;
                    }
                }
            }

            usedTiltDeg = tiltDeg;
            return false;
        }

        // 자리를 옮기고 도구 끝을 숙이고, 필요하면 관절 길을 다시 풀고, 겹침을 확인한다. 되면 빈 글자, 안 되면 이유.
        private string TryBuild(GraspTargets planned, Vector3 shift, float tiltDeg, bool isPlannedClear, out GraspTargets shifted)
        {
            shifted = Moved(planned, shift, tiltDeg, planned.Route);

            if (planned.Route != null)
            {
                if (_routes == null
                    || !_routes.TryBuild(planned.Route.Grasp, shifted.NearPose, shifted.GraspPose, shifted.RetreatPose, out TaughtJointRoute route))
                {
                    return "옮기고 숙인 자리의 관절 각도를 풀지 못해";
                }

                shifted = Moved(planned, shift, tiltDeg, route);
            }

            if (_isClear != null && isPlannedClear && !_isClear(shifted))
            {
                return "옮기면 팔이 장애물과 겹쳐";
            }

            return string.Empty;
        }

        // 세 자리를 shift만큼 옮기고, tiltDeg가 있으면 도구 끝을 숙인다.
        private static GraspTargets Moved(GraspTargets planned, Vector3 shift, float tiltDeg, TaughtJointRoute route)
        {
            GraspTargets moved = planned.ShiftedBy(shift, route);
            return tiltDeg > 0f ? moved.TiltedBy(tiltDeg, route) : moved;
        }
    }
}
