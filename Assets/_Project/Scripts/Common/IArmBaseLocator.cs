using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 로봇암 베이스(팔이 시작되는 링크)의 현재 월드 위치. 바퀴 달린 베이스가 팔을 얼마나 옮겨야 하는지 재는 데 쓴다.
    /// 베이스 루트의 위치와 팔 베이스의 위치는 회전하면 달라지므로 둘을 따로 알아야 한다.
    /// </summary>
    public interface IArmBaseLocator
    {
        Vector3 BaseWorldPosition { get; }
    }
}
