using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔 링크 하나에 붙은 충돌 상자. 링크 오브젝트 기준의 중심, 방향, 절반 크기를 담는다.
    /// 링크가 어느 관절 각도에서 어디에 있는지는 KinematicChain이 알려 주므로, 이 상자를 거기에 놓으면 임의의 자세에서 팔이 차지하는 공간을 안다.
    /// </summary>
    public readonly struct LinkVolume
    {
        public LinkVolume(int linkIndex, Vector3 localCenter, Quaternion localRotation, Vector3 halfExtents)
        {
            LinkIndex = linkIndex;
            LocalCenter = localCenter;
            LocalRotation = localRotation;
            HalfExtents = halfExtents;
        }

        /// <summary>팔 관절 링크의 순번(0부터, 베이스에서 가까운 순서).</summary>
        public int LinkIndex { get; }

        /// <summary>링크 오브젝트 기준 상자 중심.</summary>
        public Vector3 LocalCenter { get; }

        /// <summary>링크 오브젝트 기준 상자 방향.</summary>
        public Quaternion LocalRotation { get; }

        public Vector3 HalfExtents { get; }
    }
}
