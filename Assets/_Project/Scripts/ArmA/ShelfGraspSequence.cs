using System;
using System.Collections;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 선반 물건 앞쪽으로 들어가 쥐고 뒤로 빼는 순서: 그리퍼 열기 → 접근 → (카메라로 보고 쥐는 자리 고치기) → 쥐는 자리 → (도구 끝점의 실제 오차 메우기) → 재고에서 꺼내기 → 쥐기 → 후퇴.
    /// 방향은 접근, 집기, 후퇴에 모두 같다. 스스로 찾은 방향(도구 끝점 자세로 이동)과 알려 준 자세(관절 각도로 이동)가 같은 순서를 쓰고,
    /// 어느 쪽으로 움직일지는 관절 경로(route)가 있는지로만 갈린다. 움직임 한 단계는 ArmMotionSteps, 쥐는 방식은 IItemHolder,
    /// 물건 앞에서 카메라로 보고 쥐는 자리를 고치는 일은 IGraspRefiner가 맡는다(refiner가 없으면 계획한 자리 그대로 쥔다).
    /// </summary>
    public sealed class ShelfGraspSequence
    {
        private readonly IItemHolder _holder;
        private readonly ArmMotionSteps _steps;
        private readonly IGraspRefiner _refiner;

        /// <param name="refiner">물건 앞에서 쥐는 자리를 고치는 쪽. 없으면 null이고, 그러면 계획한 자리 그대로 쥔다.</param>
        public ShelfGraspSequence(IItemHolder holder, ArmMotionSteps steps, IGraspRefiner refiner = null)
        {
            _holder = holder ?? throw new ArgumentNullException(nameof(holder));
            _steps = steps ?? throw new ArgumentNullException(nameof(steps));
            _refiner = refiner;
        }

        /// <param name="route">관절 각도 경로. 있으면 접근·집기·후퇴를 그 관절 각도로 가고, 없으면(null) 도구 끝점 자세로 간다.</param>
        /// <param name="onReachedItem">물건 위치에 닿은 직후(쥐기 전)에 부른다. 필요 없으면 null.</param>
        public IEnumerator Run(
            ItemPickRequest request,
            Quaternion rotation,
            Vector3 nearPosition,
            Vector3 graspPosition,
            Vector3 retreatPosition,
            TaughtJointRoute route,
            Action onReachedItem,
            TransferResult result)
        {
            _holder.OpenGripper();

            bool isOk = false;
            yield return _steps.MoveTravel(new Pose(nearPosition, rotation), route?.Near, TransferSteps.ShelfNear, result, ok => isOk = ok);
            if (!isOk)
            {
                yield break;
            }

            // 물건 앞에 닿았으니 카메라로 물건을 보고 쥐는 자리를 고친다. 고친 자리는 쥐기와 후퇴에도 같이 쓴다.
            var targets = new GraspTargets(rotation, nearPosition, graspPosition, retreatPosition, route);
            if (_refiner != null)
            {
                GraspRefinement refinement = _refiner.Refine(request, targets);
                if (refinement.IsRejected)
                {
                    result.Fail(TransferFailure.RecognitionFailed, $"{TransferSteps.ShelfNear}: {refinement.Note}");
                    yield break;
                }

                targets = refinement.Targets;
            }

            yield return _steps.MoveGrasp(targets.GraspPose, targets.Route?.Grasp, TransferSteps.ShelfReach, result, ok => isOk = ok);
            if (!isOk)
            {
                yield break;
            }

            // 관절이 허용 오차 안에 들면 "닿았다"고 끝나는데 팔은 처져 있을 수 있다. 가라앉기를 기다렸다가 도구 끝점의 실제 오차를 메운다.
            if (_refiner is IGraspSettler settler)
            {
                for (int attempt = 0; attempt < settler.SettleAttempts; attempt++)
                {
                    yield return new WaitForSeconds(settler.SettleWaitSeconds);

                    GraspRefinement settled = settler.Settle(request, targets);
                    if (!settled.IsCorrected)
                    {
                        break;
                    }

                    targets = settled.Targets;
                    yield return _steps.MoveGrasp(targets.GraspPose, targets.Route?.Grasp, TransferSteps.ShelfReach, result, ok => isOk = ok);
                    if (!isOk)
                    {
                        yield break;
                    }
                }
            }

            onReachedItem?.Invoke();

            // 쥐는 순간에 재고를 줄인다. 이때 선반의 원본이 사라지므로 쥘 물건(복제본)은 그 뒤에 만든다.
            if (!request.TryCommit())
            {
                result.Fail(TransferFailure.CommitRejected, "쥐는 순간 재고에서 꺼내지 못했다");
                yield break;
            }

            bool isGrasped = false;
            yield return _holder.Grasp(request.Item, grasped => isGrasped = grasped);
            if (!isGrasped)
            {
                string reason = _holder.LastFailureReason;
                result.Fail(
                    TransferFailure.GraspFailed,
                    reason.Length == 0 ? "그리퍼를 닫았지만 물건을 쥐지 못했다" : $"그리퍼를 닫았지만 물건을 쥐지 못했다: {reason}");
                yield break;
            }

            yield return _steps.MoveTravel(targets.RetreatPose, targets.Route?.Retreat, TransferSteps.ShelfRetreat, result, ok => isOk = ok);
            if (!isOk)
            {
                yield break;
            }

            result.Succeed();
        }
    }
}
