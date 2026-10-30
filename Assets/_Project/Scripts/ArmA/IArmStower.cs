using System;
using System.Collections;

namespace MediKitTwin.ArmA
{
    /// <summary>팔을 몸통 가까이 접는 쪽. 베이스를 움직이기 전에 부르며, 접는 방법은 구현체가 정한다.</summary>
    public interface IArmStower
    {
        /// <summary>접히면 done(true), 접지 못하면 done(false)를 부른다.</summary>
        IEnumerator Stow(Action<bool> done);
    }
}
