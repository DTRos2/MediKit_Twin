using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>베이스 루트가 가야 할 자리와 그때의 방향(월드 Y축 기준 도).</summary>
    public readonly struct BaseGoal
    {
        public BaseGoal(Vector3 position, float yawDeg)
        {
            Position = position;
            YawDeg = yawDeg;
        }

        public Vector3 Position { get; }

        public float YawDeg { get; }
    }
}
