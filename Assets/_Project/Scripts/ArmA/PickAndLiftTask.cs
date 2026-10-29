using System;
using System.Collections;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물체 위로 접근 → 내려가기 → 쥐기 → 들어올리기를 순서대로 수행한다.
    /// IToolPoseTarget과 IGripper 인터페이스만 쓰므로 FR5, UR3 어느 쪽에도, 강화학습 환경의 스크립트 기준선으로도 쓸 수 있다.
    /// </summary>
    public class PickAndLiftTask : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _toolTarget;
        [SerializeField] private MonoBehaviour _gripper;

        [Header("동작")]
        [Tooltip("물체 중심에서 접근 시작점까지의 높이.")]
        [SerializeField, Min(0.02f)] private float _approachHeight = 0.18f;

        [Tooltip("쥘 때 TCP가 물체 중심보다 위(+)/아래(-)로 얼마나 떨어질지.")]
        [SerializeField] private float _graspHeightOffset;

        [SerializeField, Min(0.02f)] private float _liftHeight = 0.2f;

        [Header("판정")]
        [SerializeField, Min(0.001f)] private float _positionToleranceM = 0.01f;
        [SerializeField, Min(0.5f)] private float _rotationToleranceDeg = 5f;
        [SerializeField, Min(1f)] private float _stepTimeoutSeconds = 10f;
        [SerializeField, Min(0.5f)] private float _graspTimeoutSeconds = 3f;

        private IToolPoseTarget _tool;
        private IGripper _grip;
        private Coroutine _running;
        private bool _stepReached;

        public bool IsRunning => _running != null;

        /// <summary>작업이 끝났을 때 발생한다. 쥔 채 들어올렸으면 true.</summary>
        public event Action<bool> Finished;

        private void Awake()
        {
            _tool = _toolTarget as IToolPoseTarget;
            _grip = _gripper as IGripper;
            if (_tool == null || _grip == null)
            {
                Debug.LogError($"{name}: Tool Target(IToolPoseTarget)과 Gripper(IGripper)를 지정해야 한다.", this);
                enabled = false;
            }
        }

        public void Begin(Graspable target)
        {
            if (!enabled || IsRunning || target == null)
            {
                return;
            }

            _running = StartCoroutine(Run(target));
        }

        public void Cancel()
        {
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }

            _tool.ClearToolTarget();
        }

        private IEnumerator Run(Graspable target)
        {
            // 도구의 +Z가 아래를 향하고, 손가락이 벌어지는 방향은 물체의 방향을 따른다.
            Vector3 forwardOnGround = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up);
            if (forwardOnGround.sqrMagnitude < 1e-4f)
            {
                forwardOnGround = Vector3.forward;
            }

            Quaternion rotation = Quaternion.LookRotation(Vector3.down, forwardOnGround.normalized);
            Vector3 objectPosition = target.transform.position;

            _grip.Open();

            yield return MoveTool(objectPosition + Vector3.up * _approachHeight, rotation);
            if (!_stepReached)
            {
                Finish(false);
                yield break;
            }

            yield return MoveTool(objectPosition + Vector3.up * _graspHeightOffset, rotation);
            if (!_stepReached)
            {
                Finish(false);
                yield break;
            }

            _grip.Close();
            yield return WaitForGrasp();
            if (!_grip.IsHolding)
            {
                _grip.Open();
                Finish(false);
                yield break;
            }

            yield return MoveTool(objectPosition + Vector3.up * _liftHeight, rotation);
            Finish(_stepReached && _grip.IsHolding);
        }

        // 결과는 _stepReached에 담는다. 코루틴은 값을 돌려줄 수 없어서 필드를 쓴다.
        private IEnumerator MoveTool(Vector3 position, Quaternion rotation)
        {
            _stepReached = false;
            _tool.SetToolTarget(new Pose(position, rotation));

            float elapsed = 0f;
            while (elapsed < _stepTimeoutSeconds)
            {
                if (_tool.HasReachedTarget(_positionToleranceM, _rotationToleranceDeg))
                {
                    _stepReached = true;
                    yield break;
                }

                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;

                // 닿을 수 없는 목표면 기다려도 소용없으니 바로 실패로 본다.
                if (!_tool.LastSolveSucceeded)
                {
                    yield break;
                }
            }
        }

        private IEnumerator WaitForGrasp()
        {
            float elapsed = 0f;
            while (!_grip.IsHolding && elapsed < _graspTimeoutSeconds)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }
        }

        private void Finish(bool success)
        {
            _running = null;
            Finished?.Invoke(success);
        }
    }
}
