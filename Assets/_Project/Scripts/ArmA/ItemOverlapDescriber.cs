using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 물건이 지금 어느 충돌체와 얼마나 겹쳐 있는지 글로 적는다. 놓은 물건이 튕겨 나갔을 때 무엇과 부딪혔는지 가리는 데 쓴다.
    /// 트리거는 물리적으로 부딪히지 않으므로 뺀다. 물건 자신의 충돌체도 뺀다.
    /// </summary>
    public sealed class ItemOverlapDescriber
    {
        // 상자 범위를 이만큼 넓혀 조회해, 막 닿은 것까지 후보에 넣는다.
        private const float QueryMarginM = 0.005f;

        private readonly Collider[] _own;
        private readonly HashSet<Collider> _ownSet;
        private readonly StringBuilder _text = new StringBuilder();

        public ItemOverlapDescriber(Graspable item)
        {
            _own = item.Colliders;
            _ownSet = new HashSet<Collider>(_own);
        }

        /// <summary>겹친 충돌체를 "이름 깊이"로 이어 붙인 글. 겹친 것이 없으면 빈 글자다.</summary>
        public string Describe()
        {
            _text.Clear();

            foreach (Collider mine in _own)
            {
                if (mine == null || !mine.enabled)
                {
                    continue;
                }

                Bounds bounds = mine.bounds;
                Collider[] near = Physics.OverlapBox(
                    bounds.center, bounds.extents + Vector3.one * QueryMarginM, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);

                foreach (Collider other in near)
                {
                    if (_ownSet.Contains(other))
                    {
                        continue;
                    }

                    float? depth = ColliderPenetration.DepthM(mine, other);
                    if (!depth.HasValue)
                    {
                        continue;
                    }

                    if (_text.Length > 0)
                    {
                        _text.Append(", ");
                    }

                    _text.Append(ColliderLabel.Of(other)).Append(' ');
                    _text.Append(depth.Value == ColliderPenetration.UnknownDepth ? "(깊이 모름)" : $"{depth.Value * 100f:0.0}cm");
                }
            }

            return _text.ToString();
        }
    }
}
