using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 회전 관절의 한계(rad). 한계가 없는(Free) 관절은 -π~π로 본다. 순기구학 빌더와 구동부가 같은 규칙을 쓰도록 한곳에 둔다.
    /// 한계는 ArticulationBody의 twistLock과 xDrive에 들어 있다.
    /// </summary>
    public static class ArticulationJointLimits
    {
        public static bool IsLimited(ArticulationBody body)
        {
            return body.twistLock == ArticulationDofLock.LimitedMotion;
        }

        public static float LowerRad(ArticulationBody body)
        {
            return IsLimited(body) ? body.xDrive.lowerLimit * Mathf.Deg2Rad : -Mathf.PI;
        }

        public static float UpperRad(ArticulationBody body)
        {
            return IsLimited(body) ? body.xDrive.upperLimit * Mathf.Deg2Rad : Mathf.PI;
        }
    }
}
