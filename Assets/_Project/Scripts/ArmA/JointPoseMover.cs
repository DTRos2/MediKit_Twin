using System;
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.Common;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔을 관절 각도로 보내고 닿을 때까지 기다린다. ToolPoseMover의 관절 버전이다.
    /// 받을 수 없는 목표(관절 수가 다르거나 사람이 조작 중)는 기다려도 소용없으니 바로 실패로 끝낸다.
    /// 시간은 주입받아 시험에서 흐름을 직접 정한다.
    /// </summary>
    public sealed class JointPoseMover
    {
        private readonly IJointPoseTarget _target;
        private readonly Func<float> _now;
        private readonly float _timeoutSeconds;

        public JointPoseMover(IJointPoseTarget target, Func<float> now, float timeoutSeconds)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _now = now ?? throw new ArgumentNullException(nameof(now));
            _timeoutSeconds = timeoutSeconds;
        }

        /// <summary>모든 관절이 목표에서 toleranceRad 이내가 되면 done(true), 받을 수 없거나 시간이 지나면 done(false)를 부른다.</summary>
        public IEnumerator MoveTo(IReadOnlyList<float> anglesRad, float toleranceRad, Action<bool> done)
        {
            if (!_target.SetJointTarget(anglesRad))
            {
                done(false);
                yield break;
            }

            float startedAt = _now();
            while (!_target.HasReachedJointTarget(toleranceRad))
            {
                if (_now() - startedAt > _timeoutSeconds)
                {
                    done(false);
                    yield break;
                }

                yield return null;
            }

            done(true);
        }
    }
}
