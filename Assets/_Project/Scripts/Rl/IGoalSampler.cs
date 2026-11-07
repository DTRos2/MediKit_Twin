using UnityEngine;

namespace MediKitTwin.Rl
{
    /// <summary>에피소드마다 목표를 뽑는다. 닿을 수 있는 목표만 뽑는지, 선반 위 박스만 뽑는지는 구현체가 정한다.</summary>
    public interface IGoalSampler
    {
        /// <returns>월드 기준 목표 위치</returns>
        Vector3 Sample();
    }
}
