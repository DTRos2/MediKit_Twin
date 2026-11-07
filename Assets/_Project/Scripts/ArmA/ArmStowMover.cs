using System;
using System.Collections;
using MediKitTwin.Common;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 베이스가 움직이기 전에 팔을 시작 자세(접은 자세)로 모은다. 팔을 뻗은 채 베이스가 돌거나 달리면 팔이 선반에 걸려
    /// 베이스를 따라 순간이동시키는 힘과 충돌 반발이 맞서 팔이 떨리며 돈다. 이동 중에는 팔을 몸통 가까이 두어 그것을 막는다.
    /// 도구 끝점 목표(IK)가 남아 있으면 관절 목표를 덮어쓰므로 먼저 지운다. 시간은 주입받아 시험에서 흐름을 직접 정한다.
    /// </summary>
    public sealed class ArmStowMover : IArmStower
    {
        private readonly IToolPoseTarget _tool;
        private readonly IArmDriver _arm;
        private readonly Func<float> _now;
        private readonly float _toleranceRad;
        private readonly float _timeoutSeconds;

        public ArmStowMover(IToolPoseTarget tool, IArmDriver arm, Func<float> now, float toleranceRad, float timeoutSeconds)
        {
            _tool = tool ?? throw new ArgumentNullException(nameof(tool));
            _arm = arm ?? throw new ArgumentNullException(nameof(arm));
            _now = now ?? throw new ArgumentNullException(nameof(now));
            _toleranceRad = toleranceRad;
            _timeoutSeconds = timeoutSeconds;
        }

        /// <summary>접힌 자세에 닿으면 done(true), 시간이 지나도 닿지 못하면 done(false)를 부른다.</summary>
        public IEnumerator Stow(Action<bool> done)
        {
            _tool.ClearToolTarget();
            _arm.SetJointTargets(_arm.HomePoseRad);

            float startedAt = _now();
            while (!_arm.IsAtTarget(_toleranceRad))
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
