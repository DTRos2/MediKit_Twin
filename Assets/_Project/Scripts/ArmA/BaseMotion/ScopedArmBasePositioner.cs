using System;
using System.Collections;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 다른 IArmBasePositioner를 감싸서, 이동하는 동안만 범위(IMotionScope)를 켠다. 이동이 성공으로 끝나든 실패로 끝나든 되돌린다.
    /// 코루틴이 예외로 죽거나 외부에서 중단되는 경우는 finally가 실행된다고 보장할 수 없으므로 이 클래스가 보장하는 범위가 아니다.
    /// </summary>
    public sealed class ScopedArmBasePositioner : IArmBasePositioner
    {
        private readonly IArmBasePositioner _inner;
        private readonly IMotionScope _scope;

        public ScopedArmBasePositioner(IArmBasePositioner inner, IMotionScope scope)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        }

        public string LastFailureReason => _inner.LastFailureReason;

        public IEnumerator MoveArmBaseTo(Vector3 worldPosition, Action<bool> done)
        {
            _scope.Begin();
            try
            {
                yield return _inner.MoveArmBaseTo(worldPosition, done);
            }
            finally
            {
                _scope.End();
            }
        }
    }
}
