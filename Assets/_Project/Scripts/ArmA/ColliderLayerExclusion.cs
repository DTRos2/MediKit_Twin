using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물건의 충돌체가 어느 레이어와 부딪히지 않을지를 충돌체 자신의 제외 레이어(Collider.excludeLayers)로 정한다.
    /// 쥐고 가는 동안 물건이 선반, 박스, 로봇의 다른 링크에 걸려 로봇이 끌려가는 일을 막는 것이 목적이다.
    /// 충돌체 짝마다 Physics.IgnoreCollision을 거는 방식은 쓰지 않는다. 짝을 수십 개 이상 걸어 둔 채 다른 관절 몸체(팔 B)의 구동 값을
    /// 쓰면 PhysX 솔버가 첫 스텝에서 죽었다(재현으로 확인: 짝 1개는 무사, 36개와 223개와 455개는 모두 죽음). 레이어 제외는 짝을 만들지 않는다.
    /// 어떤 레이어가 있는지는 호출하는 쪽이 넘기므로 씬의 레이어 구성이 바뀌어도 이 클래스는 그대로다.
    /// </summary>
    public static class ColliderLayerExclusion
    {
        /// <summary>모든 레이어를 제외하는 마스크.</summary>
        public const int AllLayers = ~0;

        /// <summary>아무 레이어도 제외하지 않는 마스크. 레이어 행렬이 정한 대로만 부딪힌다.</summary>
        public const int NoLayers = 0;

        /// <summary>충돌체들이 놓인 레이어를 모두 합친 마스크. 충돌체가 없거나 null뿐이면 NoLayers다.</summary>
        public static int LayersOf(IReadOnlyList<Collider> colliders)
        {
            int mask = NoLayers;
            if (colliders == null)
            {
                return mask;
            }

            foreach (Collider collider in colliders)
            {
                if (collider != null)
                {
                    mask |= 1 << collider.gameObject.layer;
                }
            }

            return mask;
        }

        /// <summary>충돌체마다 layerMask에 든 레이어와는 부딪히지 않게 한다. 이전에 정한 제외는 이 값으로 바뀐다.</summary>
        /// <returns>값을 쓴 충돌체의 수.</returns>
        public static int Exclude(IReadOnlyList<Collider> colliders, int layerMask)
        {
            int count = 0;
            if (colliders == null)
            {
                return count;
            }

            foreach (Collider collider in colliders)
            {
                if (collider == null)
                {
                    continue;
                }

                collider.excludeLayers = layerMask;
                count++;
            }

            return count;
        }

        /// <summary>충돌체마다 keptLayers에 든 레이어와만 부딪히게 한다. 나머지 레이어는 모두 제외한다.</summary>
        public static int ExcludeAllExcept(IReadOnlyList<Collider> colliders, int keptLayers)
        {
            return Exclude(colliders, ~keptLayers);
        }
    }
}
