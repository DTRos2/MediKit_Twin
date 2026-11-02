using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 한 물건의 충돌체가 지정한 몇 개(손가락)를 뺀 나머지 모든 충돌체와 부딪히지 않게 한다.
    /// 손가락이 물건에 닿아 멈추는 것은 그대로 두고, 쥐고 가는 동안 선반, 박스, 로봇의 다른 링크에 걸려 로봇이 끌려가는 일을 막는다.
    /// 어떤 충돌체가 세상에 있는지는 호출하는 쪽이 넘기므로 선반 구성이 바뀌어도 이 클래스는 그대로다.
    /// </summary>
    public static class CollisionIsolation
    {
        /// <param name="subject">떼어 놓을 물건의 충돌체.</param>
        /// <param name="world">부딪히지 않게 할 후보 충돌체 전부.</param>
        /// <param name="except">계속 부딪혀야 하는 충돌체(손가락). 없으면 null.</param>
        /// <returns>부딪히지 않게 만든 짝의 수.</returns>
        public static int Isolate(IReadOnlyList<Collider> subject, IReadOnlyList<Collider> world, ICollection<Collider> except)
        {
            int count = 0;
            foreach (Collider mine in subject)
            {
                if (mine == null)
                {
                    continue;
                }

                foreach (Collider other in world)
                {
                    bool isSkipped = other == null || other == mine || (except != null && except.Contains(other));
                    if (isSkipped)
                    {
                        continue;
                    }

                    Physics.IgnoreCollision(mine, other, true);
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Isolate로 떼어 놓은 짝을 다시 부딪히게 한다. 쥐고 오는 동안은 박스에 걸리지 않게 떼어 두지만,
        /// 놓은 물건이 박스 바닥판에 내려앉고 이미 놓인 물건에 얹히려면 박스와는 다시 부딪혀야 하기 때문이다.
        /// </summary>
        /// <param name="subject">다시 부딪히게 할 물건의 충돌체.</param>
        /// <param name="targets">다시 부딪히게 할 충돌체들(예: 박스 아래의 바닥판, 벽, 이미 놓인 물건).</param>
        /// <returns>다시 부딪히게 만든 짝의 수.</returns>
        public static int Restore(IReadOnlyList<Collider> subject, IReadOnlyList<Collider> targets)
        {
            int count = 0;
            foreach (Collider mine in subject)
            {
                if (mine == null)
                {
                    continue;
                }

                foreach (Collider other in targets)
                {
                    if (other == null || other == mine)
                    {
                        continue;
                    }

                    Physics.IgnoreCollision(mine, other, false);
                    count++;
                }
            }

            return count;
        }
    }
}
