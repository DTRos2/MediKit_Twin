using System;
using System.Collections;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 다른 IArmBasePositioner를 감싸서, 베이스를 움직이기 전에 팔을 먼저 접게 한다. 접지 못하면 베이스를 움직이지 않고 실패로 끝낸다.
    /// 이동 방식이나 접는 방식을 모르고 두 추상(IArmBasePositioner, IArmStower)만 쓰므로 어떤 조합에도 붙일 수 있다.
    /// </summary>
    public sealed class StowingArmBasePositioner : IArmBasePositioner
    {
        private readonly IArmBasePositioner _inner;
        private readonly IArmStower _stower;
        private bool _isStowFailure;

        public StowingArmBasePositioner(IArmBasePositioner inner, IArmStower stower)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _stower = stower ?? throw new ArgumentNullException(nameof(stower));
        }

        public string LastFailureReason => _isStowFailure ? "이동 전에 팔을 접지 못했다" : _inner.LastFailureReason;

        public IEnumerator MoveArmBaseTo(Vector3 worldPosition, Action<bool> done)
        {
            bool isStowed = false;
            yield return _stower.Stow(stowed => isStowed = stowed);
            _isStowFailure = !isStowed;
            if (!isStowed)
            {
                done(false);
                yield break;
            }

            yield return _inner.MoveArmBaseTo(worldPosition, done);
        }
    }
}
