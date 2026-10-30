using System;
using System.Collections;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// IItemTransfer의 실제 로봇암 구현: 이동 순서만 맡는다. 바퀴 달린 베이스가 있으면 먼저 물건 앞까지 이동한 다음 집고,
    /// 쥔 채 박스 곁으로 이동해 놓는다.
    /// 선반 물건은 칸의 열린 앞쪽에서 수평으로 들어가 쥐고 뒤로 빼며(선반 칸은 위가 막혀 있다), 박스 슬롯은 위에서 내려가 놓는다.
    /// 쥐고 놓는 방식은 IItemHolder, 베이스 이동은 IArmBasePositioner, 서는 자리는 ArmBaseStandPlanner, 팔 이동은 ToolPoseMover가 맡으므로
    /// 이 클래스는 순서만 잇는다. 베이스 이동 모듈을 비우면 팔이 제자리에서 닿는 곳만 집는 고정 팔로 동작한다.
    /// </summary>
    public sealed class ArmItemTransfer : MonoBehaviour, IItemTransfer
    {
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

        [Header("박스에 놓기")]
        [Tooltip("박스 슬롯 위에서 내려가기 시작하는 높이(m).")]
        [SerializeField, Min(0.02f)] private float _boxApproachHeight = 0.15f;
        [Tooltip("물건을 놓은 뒤 들어올리는 높이(m).")]
        [SerializeField, Min(0.02f)] private float _boxLiftHeight = 0.15f;
        [Tooltip("박스 위에서 손가락이 벌어지는 방향을 정하는 기준. 도구가 아래를 볼 때 위쪽이 되는 수평 방향이다.")]
        [SerializeField] private Vector3 _boxGripYawForward = Vector3.forward;

        [Header("판정")]
        [SerializeField, Min(0.001f)] private float _positionToleranceM = 0.01f;
        [SerializeField, Min(0.5f)] private float _rotationToleranceDeg = 5f;
        [SerializeField, Min(1f)] private float _stepTimeoutSeconds = 10f;

        private IItemHolder _itemHolder;
        private IArmBasePositioner _basePositioner;
        private ArmBaseStandPlanner _standPlanner;
        private ToolPoseMover _mover;

        private void Awake()
        {
            var tool = _toolTarget as IToolPoseTarget;
            _itemHolder = _holder as IItemHolder;
            if (tool == null || _itemHolder == null)
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

                _standPlanner = new ArmBaseStandPlanner(_approach.PickStandoffM, _approach.PlaceStandoffM, _approach.BoxStandDirection);
            }

            _mover = new ToolPoseMover(tool, _positionToleranceM, _rotationToleranceDeg, _stepTimeoutSeconds);
        }

        public IEnumerator Pick(ItemPickRequest request, TransferResult result)
        {
            Vector3 itemPosition = request.Item.Pose.position;
            Vector3 outward = ShelfOutward(request.Item.ApproachDirection);

            // 먼저 물건 앞까지 이동한다. 닿는 거리 안에 서야 팔이 물건에 닿는다.
            bool isInPlace = false;
            yield return MoveBase(
                _standPlanner != null ? _standPlanner.ForShelfItem(itemPosition, outward) : (Vector3?)null,
                reached => isInPlace = reached);
            if (!isInPlace)
            {
                result.Fail(TransferFailure.MotionFailed);
                yield break;
            }

            // 도구의 +Z가 물건 쪽(칸 안쪽)을 향하게 한다. 위는 월드 위쪽으로 둔다.
            Quaternion rotation = Quaternion.LookRotation(-outward, Vector3.up);

            _itemHolder.OpenGripper();

            bool isReached = false;
            yield return _mover.MoveTo(new Pose(itemPosition + outward * _shelfApproachDistance, rotation), reached => isReached = reached);
            if (!isReached)
            {
                result.Fail(TransferFailure.MotionFailed);
                yield break;
            }

            yield return _mover.MoveTo(new Pose(itemPosition, rotation), reached => isReached = reached);
            if (!isReached)
            {
                result.Fail(TransferFailure.MotionFailed);
                yield break;
            }

            // 쥐는 순간에 재고를 줄인다. 이때 선반의 원본이 사라지므로 쥘 물건(복제본)은 그 뒤에 만든다.
            if (!request.TryCommit())
            {
                result.Fail(TransferFailure.CommitRejected);
                yield break;
            }

            bool isGrasped = false;
            yield return _itemHolder.Grasp(request.Item, grasped => isGrasped = grasped);
            if (!isGrasped)
            {
                result.Fail(TransferFailure.GraspFailed);
                yield break;
            }

            yield return _mover.MoveTo(new Pose(itemPosition + outward * _shelfRetreatDistance, rotation), reached => isReached = reached);
            if (!isReached)
            {
                result.Fail(TransferFailure.MotionFailed);
                yield break;
            }

            result.Succeed();
        }

        public IEnumerator Place(ItemPlaceRequest request, TransferResult result)
        {
            Quaternion rotation = BoxToolRotation();
            Vector3 slotPosition = request.WorldPose.position;

            // 물건을 쥔 채 지그 쪽으로 돌아가 선다. 팔은 관절 각도를 유지하므로 베이스와 함께 움직인다.
            bool isInPlace = false;
            yield return MoveBase(
                _standPlanner != null ? _standPlanner.ForBoxSlot(slotPosition) : (Vector3?)null,
                reached => isInPlace = reached);
            if (!isInPlace)
            {
                result.Fail(TransferFailure.MotionFailed);
                yield break;
            }

            bool isReached = false;
            yield return _mover.MoveTo(new Pose(slotPosition + Vector3.up * _boxApproachHeight, rotation), reached => isReached = reached);
            if (!isReached)
            {
                result.Fail(TransferFailure.MotionFailed);
                yield break;
            }

            yield return _mover.MoveTo(new Pose(slotPosition, rotation), reached => isReached = reached);
            if (!isReached)
            {
                result.Fail(TransferFailure.MotionFailed);
                yield break;
            }

            yield return _itemHolder.Release(request);

            yield return _mover.MoveTo(new Pose(slotPosition + Vector3.up * _boxLiftHeight, rotation), reached => isReached = reached);
            if (!isReached)
            {
                result.Fail(TransferFailure.MotionFailed);
                yield break;
            }

            result.Succeed();
        }

        public void Discard()
        {
            _itemHolder.Discard();
        }

        // 이동 모듈이 없으면(고정 팔) 이동 없이 도착한 것으로 본다.
        private IEnumerator MoveBase(Vector3? standPoint, Action<bool> done)
        {
            if (_basePositioner == null || !standPoint.HasValue)
            {
                done(true);
                yield break;
            }

            yield return _basePositioner.MoveArmBaseTo(standPoint.Value, done);
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
