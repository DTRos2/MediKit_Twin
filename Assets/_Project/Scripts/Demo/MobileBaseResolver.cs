using MediKitTwin.ArmA;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 로봇암이 실려 있는 굴러가는 베이스(IMobileBase)를 찾는다. 베이스가 팔의 부모 계층에 있으면 그것을 쓰고,
    /// 팔과 베이스가 형제 오브젝트이면 이동 모듈(ArmBasePositionerBehaviour)이 그 팔과 짝지은 베이스를 쓴다.
    /// 어느 쪽도 아니면 null이다(바닥에 고정된 팔). 씬에 베이스가 하나뿐이라고 아무 팔에나 붙이지는 않는다.
    /// </summary>
    public static class MobileBaseResolver
    {
        public static IMobileBase Resolve(MonoBehaviour robot)
        {
            IMobileBase inParents = robot.GetComponentInParent<IMobileBase>();
            if (inParents != null)
            {
                return inParents;
            }

            foreach (ArmBasePositionerBehaviour positioner in Object.FindObjectsByType<ArmBasePositionerBehaviour>(FindObjectsInactive.Exclude))
            {
                if (positioner.ArmBaseSource == robot)
                {
                    return positioner.MobileBase;
                }
            }

            return null;
        }
    }
}
