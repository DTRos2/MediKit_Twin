using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// FixedUpdate마다 IPhysicsStepSource의 알림을 낸다. 아무도 듣지 않으면 하는 일이 없다.
    /// 코루틴의 WaitForFixedUpdate는 한 코루틴에 하나씩만 기다릴 수 있어, 베이스를 옮기는 코루틴과 나란히 재려면 별도 알림이 필요하다.
    /// </summary>
    public sealed class PhysicsStepTicker : MonoBehaviour, IPhysicsStepSource
    {
        public event Action Stepped;

        private void FixedUpdate()
        {
            Stepped?.Invoke();
        }
    }
}
