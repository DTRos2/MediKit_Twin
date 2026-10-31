using System;
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// IItemTransfer의 실제 로봇암 구현: 이동 순서만 맡는다. 바퀴 달린 베이스가 있으면 먼저 물건 앞까지 이동한 다음 집고,
    /// 쥔 채 박스 곁으로 이동해 놓는다.
    /// 서는 자리에 도착하면 팔을 움직이기 전에, 기본 도구 방향에서 조금씩 기울이고 돌리고 비튼 방향 중 접근, 집기, 후퇴 위치에
    /// 모두 닿는 방향을 찾아(GraspOrientationSearcher) 그 방향으로 집는다. 어느 방향도 닿지 않으면 서는 거리를 바꿔 다시 선다.
    /// 선반 물건은 칸의 열린 앞쪽에서 들어가 쥐고 뒤로 빼며(선반 칸은 위가 막혀 있다), 박스 슬롯은 위에서 내려가 놓는다.
    /// 쥐고 놓는 방식은 IItemHolder, 베이스 이동은 IArmBasePositioner, 서는 자리는 ArmBaseStandPlanner, 팔 이동은 ToolPoseMover가 맡으므로
    /// 이 클래스는 순서만 잇는다. 베이스 이동 모듈을 비우면 팔이 제자리에서 닿는 곳만 집는 고정 팔로 동작한다.
    /// 실패하면 어느 단계에서 왜 실패했는지를 결과(TransferResult.Detail)에 남긴다.
    /// </summary>
    public sealed class ArmItemTransfer : MonoBehaviour, IItemTransfer
    {
        // 실패 설명에 쓰는 단계 이름.
        private const string StepShelfApproachWaypoint = "선반 칸 정면 경유지로 이동";
        private const string StepShelfStandPoint = "선반 앞 서는 자리로 이동";
        private const string StepShelfNear = "물건 앞쪽으로 팔 뻗기";
        private const string StepShelfReach = "물건 위치로 팔 뻗기";
        private const string StepShelfRetreat = "물건을 쥔 채 뒤로 빼기";
        private const string StepBoxStandPoint = "박스 곁 서는 자리로 이동";
        private const string StepBoxAbove = "박스 슬롯 위로 팔 뻗기";
        private const string StepBoxSlot = "박스 슬롯으로 내리기";
        private const string StepBoxLift = "놓은 뒤 들어올리기";

        // 베이스를 움직이지 않는 고정 팔은 보정 없이 한 번만 시도한다.
        private static readonly float[] SingleAttempt = { 0f };

        // 한 번의 "서고, 닿는 방향을 찾는" 시도에 필요한 것들을 묶는다. 집기와 놓기가 같은 절차를 쓰기 위해서다.
        private readonly struct ReachPlan
        {
            public ReachPlan(
                IReadOnlyList<float> standoffOffsets,
                Func<float, Vector3?> waypointFor,
                Func<float, Vector3?> standFor,
                Quaternion nominal,
                IReadOnlyList<Vector3> positions,
                OrientationSearchSettings search,
                string waypointStep,
                string standStep,
                string reachStep,
                float? facingYawDeg)
            {
                StandoffOffsets = standoffOffsets;
                WaypointFor = waypointFor;
                StandFor = standFor;
                Nominal = nominal;
                Positions = positions;
                Search = search;
                WaypointStep = waypointStep;
                StandStep = standStep;
                ReachStep = reachStep;
                FacingYawDeg = facingYawDeg;
            }

            public IReadOnlyList<float> StandoffOffsets { get; }

            /// <summary>서는 자리로 들어가기 전에 거치는 경유지. 없으면 null.</summary>
            public Func<float, Vector3?> WaypointFor { get; }

            public Func<float, Vector3?> StandFor { get; }

            public Quaternion Nominal { get; }

            public IReadOnlyList<Vector3> Positions { get; }

            public OrientationSearchSettings Search { get; }

            public string WaypointStep { get; }

            public string StandStep { get; }

            public string ReachStep { get; }

            /// <summary>서는 자리에서 바라볼 방향(월드 Y축 기준 도). 없으면 마지막 이동 방향으로 선다.</summary>
            public float? FacingYawDeg { get; }
        }

        [Tooltip("도구 끝점 목표를 받는 쪽(IToolPoseTarget 구현체, 보통 IkArmTarget).")]
        [SerializeField] private MonoBehaviour _toolTarget;
        [Tooltip("물건을 쥐고 놓는 방식(IItemHolder 구현체: ScriptAttachHolder 또는 PhysicsGraspHolder).")]
        [SerializeField] private MonoBehaviour _holder;

        [Header("베이스 이동 (비우면 고정 팔)")]
        [Tooltip("팔 베이스를 옮기는 쪽(IArmBasePositioner 구현체, 보통 ArmBasePositionerBehaviour). 비우면 이동하지 않는다.")]
        [SerializeField] private MonoBehaviour _positioner;
        [Tooltip("서는 거리와 방향을 담은 설정. 베이스 이동 모듈을 연결했다면 반드시 지정한다.")]
        [SerializeField] private ArmBaseApproachConfigSO _approach;

        [Header("선반에서 집기")]
        [Tooltip("선반 칸 앞에서 물건 쪽으로 들어가기 시작하는 거리(m).")]
        [SerializeField, Min(0.02f)] private float _shelfApproachDistance = 0.15f;
        [Tooltip("물건을 쥔 뒤 칸 밖으로 빼내는 거리(m).")]
        [SerializeField, Min(0.02f)] private float _shelfRetreatDistance = 0.15f;
        [Tooltip("물건에 닿는 도구 방향을 찾을 때 기본 방향(수평으로 칸 안쪽)에서 벗어날 수 있는 범위. 선반 칸은 위가 막혀 있어 위로는 조금만 든다.")]
        [SerializeField] private OrientationSearchSettings _shelfSearch = new OrientationSearchSettings(45f, 15f, 15f, 30f, 15f, 90f, 90f);

        [Header("박스에 놓기")]
        [Tooltip("박스 슬롯 위에서 내려가기 시작하는 높이(m).")]
        [SerializeField, Min(0.02f)] private float _boxApproachHeight = 0.15f;
        [Tooltip("물건을 놓은 뒤 들어올리는 높이(m).")]
        [SerializeField, Min(0.02f)] private float _boxLiftHeight = 0.15f;
        [Tooltip("박스 위에서 손가락이 벌어지는 방향을 정하는 기준. 도구가 아래를 볼 때 위쪽이 되는 수평 방향이다.")]
        [SerializeField] private Vector3 _boxGripYawForward = Vector3.forward;
        [Tooltip("박스 슬롯에 닿는 도구 방향을 찾을 때 기본 방향(똑바로 아래)에서 벗어날 수 있는 범위. 비틀기는 위에서 보아 손을 돌리는 것이다.")]
        [SerializeField] private OrientationSearchSettings _boxSearch = new OrientationSearchSettings(30f, 30f, 15f, 30f, 15f, 180f, 90f);

        [Header("판정 — 쥐고 놓는 동작 (엄격)")]
        [Tooltip("물건을 쥐는 자리와 박스 슬롯에 놓는 자리에서 허용하는 위치 오차(m).")]
        [SerializeField, Min(0.001f)] private float _positionToleranceM = 0.01f;
        [Tooltip("물건을 쥐는 자리와 박스 슬롯에 놓는 자리에서 허용하는 회전 오차(도).")]
        [SerializeField, Min(0.5f)] private float _rotationToleranceDeg = 5f;

        [Header("판정 — 접근·후퇴·들어올리기 (느슨)")]
        [Tooltip("물건 앞, 박스 위처럼 지나가는 자리에서 허용하는 위치 오차(m). 시간 초과로 실패하면 키운다.")]
        [SerializeField, Min(0.001f)] private float _travelPositionToleranceM = 0.03f;
        [Tooltip("지나가는 자리에서 허용하는 회전 오차(도). 시간 초과로 실패하면 키운다.")]
        [SerializeField, Min(0.5f)] private float _travelRotationToleranceDeg = 10f;

        [Header("판정 — 시간")]
        [Tooltip("한 동작이 목표에 닿기를 기다리는 최대 시간(초).")]
        [SerializeField, Min(1f)] private float _stepTimeoutSeconds = 10f;

        private IToolPoseTarget _tool;
        private IItemHolder _itemHolder;
        private IArmBasePositioner _basePositioner;
        private ArmBaseStandPlanner _standPlanner;
        private ToolPoseMover _mover;
        private GraspOrientationSearcher _searcher;

        private void Awake()
        {
            _tool = _toolTarget as IToolPoseTarget;
            _itemHolder = _holder as IItemHolder;
            if (_tool == null || _itemHolder == null)
            {
                Debug.LogError($"{name}: Tool Target(IToolPoseTarget)과 Holder(IItemHolder)를 지정해야 한다.", this);
                enabled = false;
                return;
            }

            _basePositioner = _positioner as IArmBasePositioner;
            if (_basePositioner != null)
            {
                if (_approach == null)
                {
                    Debug.LogError($"{name}: 베이스 이동 모듈을 연결했으면 Approach 설정도 지정해야 한다.", this);
                    enabled = false;
                    return;
                }

                _standPlanner = new ArmBaseStandPlanner(
                    _approach.PickStandoffM, _approach.PlaceStandoffM, _approach.ApproachExtraM, _approach.BoxStandDirection);
            }

            _mover = new ToolPoseMover(_tool, _travelPositionToleranceM, _travelRotationToleranceDeg, _stepTimeoutSeconds);
            _searcher = new GraspOrientationSearcher(pose => _tool.Probe(pose).Converged);
        }

        public IEnumerator Pick(ItemPickRequest request, TransferResult result)
        {
            Vector3 itemPosition = request.Item.Pose.position;
            Vector3 outward = ShelfOutward(request.Item.ApproachDirection);
            Vector3 nearPosition = itemPosition + outward * _shelfApproachDistance;
            Vector3 retreatPosition = itemPosition + outward * _shelfRetreatDistance;

            // 도구의 +Z가 물건 쪽(칸 안쪽)을 향하는 것이 기본 방향이다. 위는 월드 위쪽으로 둔다.
            Quaternion nominal = Quaternion.LookRotation(-outward, Vector3.up);

            var plan = new ReachPlan(
                _standPlanner != null ? _approach.PickStandoffOffsetsM : (IReadOnlyList<float>)SingleAttempt,
                extra => _standPlanner != null ? _standPlanner.ForShelfItemApproach(itemPosition, outward, extra) : (Vector3?)null,
                extra => _standPlanner != null ? _standPlanner.ForShelfItem(itemPosition, outward, extra) : (Vector3?)null,
                nominal,
                new[] { nearPosition, itemPosition, retreatPosition },
                _shelfSearch,
                StepShelfApproachWaypoint,
                StepShelfStandPoint,
                StepShelfNear,
                FacingYawFor(ArmBaseStandPlanner.ShelfItemFacingYawDeg(outward, FacingOffsetDeg())));

            // 서는 자리에 도착하면 팔이 닿는 방향을 찾는다. 찾은 방향으로 접근, 집기, 후퇴를 모두 한다.
            var search = new OrientationSearchResult();
            yield return ApproachAndSearch(plan, result, found => search = found);
            if (!search.IsFound)
            {
                yield break;
            }

            Quaternion rotation = search.Rotation;
            _itemHolder.OpenGripper();

            bool isOk = false;
            yield return MoveTool(new Pose(nearPosition, rotation), StepShelfNear, result, ok => isOk = ok);
            if (!isOk)
            {
                yield break;
            }

            yield return MoveToolPrecisely(new Pose(itemPosition, rotation), StepShelfReach, result, ok => isOk = ok);
            if (!isOk)
            {
                yield break;
            }

            // 쥐는 순간에 재고를 줄인다. 이때 선반의 원본이 사라지므로 쥘 물건(복제본)은 그 뒤에 만든다.
            if (!request.TryCommit())
            {
                result.Fail(TransferFailure.CommitRejected, "쥐는 순간 재고에서 꺼내지 못했다");
                yield break;
            }

            bool isGrasped = false;
            yield return _itemHolder.Grasp(request.Item, grasped => isGrasped = grasped);
            if (!isGrasped)
            {
                result.Fail(TransferFailure.GraspFailed, "그리퍼를 닫았지만 물건을 쥐지 못했다");
                yield break;
            }

            yield return MoveTool(new Pose(retreatPosition, rotation), StepShelfRetreat, result, ok => isOk = ok);
            if (!isOk)
            {
                yield break;
            }

            result.Succeed();
        }

        public IEnumerator Place(ItemPlaceRequest request, TransferResult result)
        {
            Vector3 slotPosition = request.WorldPose.position;
            Vector3 abovePosition = slotPosition + Vector3.up * _boxApproachHeight;
            Vector3 liftPosition = slotPosition + Vector3.up * _boxLiftHeight;

            // 물건을 쥔 채 지그 쪽으로 돌아가 선다. 팔은 관절 각도를 유지하므로 베이스와 함께 움직인다.
            var plan = new ReachPlan(
                _standPlanner != null ? _approach.PlaceStandoffOffsetsM : (IReadOnlyList<float>)SingleAttempt,
                null,
                extra => _standPlanner != null ? _standPlanner.ForBoxSlot(slotPosition, extra) : (Vector3?)null,
                BoxToolRotation(),
                new[] { abovePosition, slotPosition, liftPosition },
                _boxSearch,
                string.Empty,
                StepBoxStandPoint,
                StepBoxAbove,
                FacingYawFor(_standPlanner?.BoxSlotFacingYawDeg(FacingOffsetDeg())));

            var search = new OrientationSearchResult();
            yield return ApproachAndSearch(plan, result, found => search = found);
            if (!search.IsFound)
            {
                yield break;
            }

            Quaternion rotation = search.Rotation;

            bool isOk = false;
            yield return MoveTool(new Pose(abovePosition, rotation), StepBoxAbove, result, ok => isOk = ok);
            if (!isOk)
            {
                yield break;
            }

            yield return MoveToolPrecisely(new Pose(slotPosition, rotation), StepBoxSlot, result, ok => isOk = ok);
            if (!isOk)
            {
                yield break;
            }

            yield return _itemHolder.Release(request);

            yield return MoveTool(new Pose(liftPosition, rotation), StepBoxLift, result, ok => isOk = ok);
            if (!isOk)
            {
                yield break;
            }

            result.Succeed();
        }

        public void Discard()
        {
            _itemHolder.Discard();
        }

        // 서서 닿는 도구 방향을 찾는다. 하나도 닿지 않으면 서는 거리를 계획의 보정값만큼 늘려 다시 서서 찾는다.
        // 베이스가 못 가는 경우는 거리를 바꿔도 소용없으므로 곧바로 끝낸다. 실패하면 결과에 이유가 남고 done에는 못 찾은 결과를 넘긴다.
        private IEnumerator ApproachAndSearch(ReachPlan plan, TransferResult result, Action<OrientationSearchResult> done)
        {
            for (int attempt = 0; attempt < plan.StandoffOffsets.Count; attempt++)
            {
                float extra = plan.StandoffOffsets[attempt];
                bool isOk = false;

                if (plan.WaypointFor != null)
                {
                    yield return MoveBase(plan.WaypointFor(extra), plan.FacingYawDeg, plan.WaypointStep, result, ok => isOk = ok);
                    if (!isOk)
                    {
                        done(new OrientationSearchResult());
                        yield break;
                    }
                }

                yield return MoveBase(plan.StandFor(extra), plan.FacingYawDeg, plan.StandStep, result, ok => isOk = ok);
                if (!isOk)
                {
                    done(new OrientationSearchResult());
                    yield break;
                }

                var search = new OrientationSearchResult();
                yield return _searcher.Search(plan.Nominal, plan.Positions, plan.Search, search);
                if (search.IsFound)
                {
                    if (search.DeviationDeg > 0f)
                    {
                        Debug.Log($"{name}: 기본 방향은 닿지 않아 {search.DeviationDeg:0}도 벌린 방향으로 {plan.ReachStep}({search.TriedCount}번째 후보).", this);
                    }

                    done(search);
                    yield break;
                }

                result.Fail(
                    TransferFailure.MotionFailed,
                    $"{plan.ReachStep}: 서는 자리에서 팔이 닿는 방향을 {search.TriedCount}가지 찾아봤지만 없었다 ({DescribeReach(plan)})");
            }

            if (plan.StandoffOffsets.Count > 1)
            {
                result.Fail(TransferFailure.MotionFailed, $"{result.Detail} (서는 거리를 {plan.StandoffOffsets.Count}번 바꿔 시도함)");
            }

            done(new OrientationSearchResult());
        }

        // 이동 모듈이 없으면(고정 팔) 이동 없이 도착한 것으로 본다. 실패하면 단계 이름과 함께 결과에 남긴다.
        private IEnumerator MoveBase(Vector3? standPoint, float? facingYawDeg, string step, TransferResult result, Action<bool> done)
        {
            if (_basePositioner == null || !standPoint.HasValue)
            {
                done(true);
                yield break;
            }

            bool isArrived = false;
            yield return facingYawDeg.HasValue
                ? _basePositioner.MoveArmBaseTo(standPoint.Value, facingYawDeg.Value, arrived => isArrived = arrived)
                : _basePositioner.MoveArmBaseTo(standPoint.Value, arrived => isArrived = arrived);
            if (!isArrived)
            {
                result.Fail(TransferFailure.MotionFailed, $"{step}: 베이스가 서는 자리에 도착하지 못했다 ({_basePositioner.LastFailureReason})");
            }

            done(isArrived);
        }

        // 지나가는 자리(접근, 후퇴, 들어올리기)는 느슨한 오차로 판정한다.
        private IEnumerator MoveTool(Pose target, string step, TransferResult result, Action<bool> done)
        {
            return MoveToolWithin(target, _travelPositionToleranceM, _travelRotationToleranceDeg, step, result, done);
        }

        // 쥐고 놓는 자리는 엄격한 오차로 판정한다.
        private IEnumerator MoveToolPrecisely(Pose target, string step, TransferResult result, Action<bool> done)
        {
            return MoveToolWithin(target, _positionToleranceM, _rotationToleranceDeg, step, result, done);
        }

        // 도구 끝점을 목표로 보낸다. 방향은 미리 닿는 것을 확인했으므로, 여기서 실패하면 무언가에 걸렸거나 시간이 모자란 것이다.
        private IEnumerator MoveToolWithin(
            Pose target, float positionToleranceM, float rotationToleranceDeg, string step, TransferResult result, Action<bool> done)
        {
            bool isReached = false;
            yield return _mover.MoveTo(target, positionToleranceM, rotationToleranceDeg, reached => isReached = reached);
            if (!isReached)
            {
                result.Fail(TransferFailure.MotionFailed, $"{step}: {DescribeToolFailure()}");
            }

            done(isReached);
        }

        // 기본 방향으로 각 지점에 닿는 정도를 적는다. 오차가 크면 팔이 닿는 범위 밖이고,
        // 오차가 거의 없는데 못 닿았다면 현재 자세에서 너무 멀리 돌아야만 닿는 것이다(IK의 관절 이동 제한).
        private string DescribeReach(ReachPlan plan)
        {
            var parts = new string[plan.Positions.Count];
            for (int i = 0; i < parts.Length; i++)
            {
                IkResult ik = _tool.Probe(new Pose(plan.Positions[i], plan.Nominal));
                bool isFarOnly = !ik.Converged && ik.PositionError <= _positionToleranceM;
                string reach = isFarOnly
                    ? "현재 자세에서 너무 멀리 돌아야 닿음"
                    : $"오차 {ik.PositionError:F2}m/{ik.RotationErrorDeg:F0}도";
                parts[i] = $"지점{i + 1}: {reach}{DescribePlacement(plan.Positions[i])}";
            }

            return string.Join(", ", parts);
        }

        // 팔 베이스에서 본 지점의 수평 거리와 높이 차. 팔의 닿는 반경과 견주어 거리 문제인지 높이 문제인지 가려내는 데 쓴다.
        private string DescribePlacement(Vector3 worldPoint)
        {
            if (!(_toolTarget is IToolReachFrame frame))
            {
                return string.Empty;
            }

            Vector3 offset = frame.OffsetFromArmBase(worldPoint);
            float height = offset.y;
            float horizontal = new Vector2(offset.x, offset.z).magnitude;
            return $" [팔 베이스에서 수평 {horizontal:F2}m, 높이 {height:+0.00;-0.00}m]";
        }

        private string DescribeToolFailure()
        {
            if (_tool.LastSolveSucceeded)
            {
                return "팔이 시간 안에 목표 자세에 닿지 못했다(무언가에 걸렸거나 너무 느림)";
            }

            IkResult ik = _tool.LastResult;
            return $"팔이 닿는 자세를 찾지 못했다(IK 위치 오차 {ik.PositionError:F3}m, 회전 오차 {ik.RotationErrorDeg:F1}도)";
        }

        // 구역을 정면으로 보고 서는 설정이 꺼져 있으면 방향을 정하지 않는다.
        private float? FacingYawFor(float? facingYawDeg)
        {
            return _approach != null && _approach.FaceTargetAtStand ? facingYawDeg : null;
        }

        private float FacingOffsetDeg()
        {
            return _approach != null ? _approach.FacingOffsetDeg : 0f;
        }

        // 칸 방향을 알 수 없으면(영벡터) 월드 앞쪽에서 들어간다. 수평 성분만 쓴다.
        private static Vector3 ShelfOutward(Vector3 direction)
        {
            Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
            return flat.sqrMagnitude < 1e-4f ? Vector3.back : flat.normalized;
        }

        private Quaternion BoxToolRotation()
        {
            Vector3 forward = Vector3.ProjectOnPlane(_boxGripYawForward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f)
            {
                forward = Vector3.forward;
            }

            return Quaternion.LookRotation(Vector3.down, forward.normalized);
        }
    }
}
