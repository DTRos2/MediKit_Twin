using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 선반 물건의 지금 실제 자세. 집기 요청에 담긴 자세(ShelfPickItem.Pose)는 요청한 순간의 값이라,
    /// 로봇이 가는 사이 물건이 움직였으면 달라진다. 선반의 원본 오브젝트가 아직 있으면 그 지금 자세를, 없으면 요청 자세를 쓴다.
    /// </summary>
    public static class ShelfItemLivePose
    {
        public static Pose Of(ShelfPickItem item)
        {
            GameObject source = item.Prototype;
            if (source == null || !source.activeInHierarchy)
            {
                return item.Pose;
            }

            return new Pose(source.transform.position, source.transform.rotation);
        }
    }
}
