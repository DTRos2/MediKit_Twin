using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 두 레이어 사이의 충돌을 범위 동안만 끈다. 끝나면 시작 전 상태로 되돌리므로, 원래 꺼 둔 쌍을 실수로 켜지 않는다.
    /// 레이어 이름은 모르고 번호만 받는다(이름 해석은 만드는 쪽).
    /// </summary>
    public sealed class LayerCollisionIgnoreScope : IMotionScope
    {
        private readonly int _layerA;
        private readonly int _layerB;
        private bool _wasIgnored;

        public LayerCollisionIgnoreScope(int layerA, int layerB)
        {
            _layerA = layerA;
            _layerB = layerB;
        }

        public void Begin()
        {
            _wasIgnored = Physics.GetIgnoreLayerCollision(_layerA, _layerB);
            Physics.IgnoreLayerCollision(_layerA, _layerB, true);
        }

        public void End()
        {
            Physics.IgnoreLayerCollision(_layerA, _layerB, _wasIgnored);
        }
    }
}
