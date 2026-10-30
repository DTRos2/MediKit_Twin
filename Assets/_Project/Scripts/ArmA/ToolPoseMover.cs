using System;
using System.Collections;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 도구 끝점을 목표 자세로 보내고 닿을 때까지 기다린다. IToolPoseTarget만 쓰므로 FR5, UR3 어느 쪽에도 쓸 수 있다.
    /// 닿지 못하는 목표는 기다려도 소용없으니 IK가 실패하면 바로 끝낸다.
    /// </summary>
    public sealed class ToolPoseMover
    {
        private readonly IToolPoseTarget _tool;
        private readonly float _positionToleranceM;
        private readonly float _rotationToleranceDeg;
        private readonly float _timeoutSeconds;

        public ToolPoseMover(IToolPoseTarget tool, float positionToleranceM, float rotationToleranceDeg, float timeoutSeconds)
        {
            _tool = tool ?? throw new ArgumentNullException(nameof(tool));
            _positionToleranceM = positionToleranceM;
            _rotationToleranceDeg = rotationToleranceDeg;
            _timeoutSeconds = timeoutSeconds;
        }

        /// <summary>목표에 닿으면 done(true), 시간이 지나거나 닿을 수 없으면 done(false)를 부른다.</summary>
        public IEnumerator MoveTo(Pose target, Action<bool> done)
        {
            _tool.SetToolTarget(target);

            float elapsed = 0f;
            while (elapsed < _timeoutSeconds)
            {
                if (_tool.HasReachedTarget(_positionToleranceM, _rotationToleranceDeg))
                {
                    done(true);
                    yield break;
                }

                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;

                if (!_tool.LastSolveSucceeded)
                {
                    break;
                }
            }

            done(false);
        }
    }
}
