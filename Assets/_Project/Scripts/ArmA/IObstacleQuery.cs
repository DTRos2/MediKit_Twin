using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>월드에 놓인 상자가 장애물과 겹치는지 묻는다. 물리 엔진으로 묻는 구현과 시험용 가짜 구현을 같은 방식으로 쓴다.</summary>
    public interface IObstacleQuery
    {
        /// <param name="obstacleName">겹쳤으면 겹친 장애물의 이름. 겹치지 않으면 빈 글자.</param>
        /// <returns>상자가 장애물과 겹치면 true.</returns>
        bool Overlaps(Vector3 center, Vector3 halfExtents, Quaternion rotation, out string obstacleName);
    }
}
