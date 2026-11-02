using System;
using System.Collections;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 다른 IArmBasePositioner를 감싸서, 베이스를 움직이기 전에 팔을 먼저 접게 한다. 접지 못하면 베이스를 움직이지 않고 실패로 끝낸다.
    /// 이동 방식이나 접는 방식을 모르고 두 추상(IArmBasePositioner, IArmStower)만 쓰므로 어떤 조합에도 붙일 수 있다.
    /// 접은 뒤와 도착한 뒤에 단계를 알린다(IArmPhaseSource). 쥔 물건이 접는 동안 밀리는지, 옮기는 동안 밀리는지 가르는 데 쓴다.
    /// </summary>
    public sealed class StowingArmBasePositioner : IArmBasePositioner, IArmPhaseSource
    {
        private readonly IArmBasePositioner _inner;
        private readonly IArmStower _stower;
        private bool _isStowFailure;

        public StowingArmBasePositioner(IArmBasePositioner inner, IArmStower stower)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _stower = stower ?? throw new ArgumentNullException(nameof(stower));
        }

        public event Action<ArmMotionPhase> PhaseReached;

        public string LastFailureReason => _isStowFailure ? "이동 전에 팔을 접지 못했다" : _inner.LastFailureReason;

        public IEnumerator MoveArmBaseTo(Vector3 worldPosition, Action<bool> done)
        {
            return StowThen(() => _inner.MoveArmBaseTo(worldPosition, ReportArrival(done)), done);
        }

        public IEnumerator MoveArmBaseTo(Vector3 worldPosition, float facingYawDeg, Action<bool> done)
        {
            return StowThen(() => _inner.MoveArmBaseTo(worldPosition, facingYawDeg, ReportArrival(done)), done);
        }

        // 팔을 접은 뒤에야 안쪽 이동을 만들어 시작한다. 접지 못하면 이동하지 않는다.
        private IEnumerator StowThen(Func<IEnumerator> move, Action<bool> done)
        {
            bool isStowed = false;
            yield return _stower.Stow(stowed => isStowed = stowed);
            _isStowFailure = !isStowed;
            if (!isStowed)
            {
                done(false);
                yield break;
            }

            PhaseReached?.Invoke(ArmMotionPhase.Stowed);
            yield return move();
        }

        // 도착했을 때만 알린다. 이동에 실패했으면 도착한 것이 아니다.
        private Action<bool> ReportArrival(Action<bool> done)
        {
            return arrived =>
            {
                if (arrived)
                {
                    PhaseReached?.Invoke(ArmMotionPhase.BaseArrived);
                }

                done(arrived);
            };
        }
    }
}
