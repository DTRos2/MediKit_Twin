using System;
using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 로봇이 얹힌 베이스가 움직여 로봇을 통째로 옮길 때마다 그 이동을 알린다.
    /// 로봇의 물리 몸체는 한꺼번에 옮겨지지만, 몸체에 조인트로만 붙은 것(쥔 물건)은 물리가 다음 단계에서야 따라와서 한 프레임 늦는다.
    /// 그래서 쥔 물건 쪽이 이 알림을 받아 같은 이동을 바로 따라간다. 베이스가 어떻게 움직이는지는 알려 하지 않고 이동량만 받는다.
    /// </summary>
    public interface IMountMotionSource
    {
        /// <summary>로봇이 옮겨질 때마다 부른다. 값은 옮기기 전 자세를 옮긴 뒤 자세로 보내는 월드 변환이다.</summary>
        event Action<Matrix4x4> Moved;
    }
}
