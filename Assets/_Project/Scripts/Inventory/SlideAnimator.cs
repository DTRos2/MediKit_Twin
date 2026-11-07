using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Inventory
{
    /// <summary>
    /// 물건을 목표 위치까지 일정한 속도로 미끄러뜨린다. 칸 화면(ShelfCellView)이 어떤 물건을 언제 움직일지만 정하고,
    /// 실제로 움직이는 일은 이 클래스가 맡아 화면 클래스가 규칙과 애니메이션을 함께 갖지 않게 한다.
    /// 시간은 호출하는 쪽이 주는 경과 시간(deltaTime)만 쓰므로 시뮬레이션 시간(배속)을 그대로 따른다.
    /// </summary>
    public sealed class SlideAnimator
    {
        private readonly float _speedMetersPerSecond;
        private readonly List<Transform> _sliding = new List<Transform>();
        private readonly Dictionary<Transform, Vector3> _localTargets = new Dictionary<Transform, Vector3>();

        public SlideAnimator(float speedMetersPerSecond)
        {
            _speedMetersPerSecond = speedMetersPerSecond;
        }

        public bool IsSliding => _sliding.Count > 0;

        /// <summary>물건을 부모 기준 로컬 위치 localTarget까지 미끄러뜨리기 시작한다. 이미 움직이는 중이면 목표만 바꾼다.</summary>
        public void SlideTo(Transform item, Vector3 localTarget)
        {
            _localTargets[item] = localTarget;
            if (!_sliding.Contains(item))
            {
                _sliding.Add(item);
            }
        }

        /// <summary>물건의 움직임을 멈춘다(숨기거나 없앨 물건에 쓴다). 움직이지 않는 물건이면 아무것도 하지 않는다.</summary>
        public void Stop(Transform item)
        {
            _sliding.Remove(item);
            _localTargets.Remove(item);
        }

        /// <summary>경과 시간만큼 움직인다. 목표에 닿은 물건은 목록에서 뺀다.</summary>
        public void Step(float deltaTime)
        {
            for (int i = _sliding.Count - 1; i >= 0; i--)
            {
                Transform item = _sliding[i];
                Vector3 target = _localTargets[item];
                item.localPosition = Vector3.MoveTowards(item.localPosition, target, _speedMetersPerSecond * deltaTime);
                if (item.localPosition == target)
                {
                    Stop(item);
                }
            }
        }
    }
}
