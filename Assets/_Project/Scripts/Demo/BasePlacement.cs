using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>굴러가는 베이스가 바닥 위에 놓인 자리: 월드 위치와 월드 Y축 기준 방향(도).</summary>
    public readonly struct BasePlacement
    {
        public BasePlacement(Vector3 position, float yawDeg)
        {
            Position = position;
            YawDeg = yawDeg;
        }

        public Vector3 Position { get; }

        public float YawDeg { get; }
    }
}
