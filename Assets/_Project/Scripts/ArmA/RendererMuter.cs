using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 정해 둔 렌더러들을 잠깐 그리지 않게 했다가 원래대로 돌린다(Renderer.forceRenderingOff).
    /// 레이어나 물리 설정은 건드리지 않아 충돌에 영향이 없다. 언제 끌지는 모른다(ToolCameraFeed가 카메라를 직접 그리는 순간에만 부른다).
    /// 이미 꺼져 있던 렌더러는 Restore 뒤에도 꺼진 채로 둔다. 지워진 렌더러는 건너뛴다.
    /// </summary>
    public sealed class RendererMuter
    {
        private readonly Renderer[] _renderers;
        private readonly bool[] _wasOff;
        private bool _muted;

        public RendererMuter(IEnumerable<Renderer> renderers)
        {
            var list = new List<Renderer>();
            if (renderers != null)
            {
                foreach (Renderer renderer in renderers)
                {
                    if (renderer != null)
                    {
                        list.Add(renderer);
                    }
                }
            }

            _renderers = list.ToArray();
            _wasOff = new bool[_renderers.Length];
        }

        public int Count => _renderers.Length;

        public bool IsMuted => _muted;

        /// <summary>렌더러들을 그리지 않게 한다. 이미 꺼 둔 상태에서 또 부르면 아무 일도 하지 않는다.</summary>
        public void Mute()
        {
            if (_muted)
            {
                return;
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                {
                    continue;
                }

                _wasOff[i] = _renderers[i].forceRenderingOff;
                _renderers[i].forceRenderingOff = true;
            }

            _muted = true;
        }

        /// <summary>Mute 전의 상태로 돌린다. Mute하지 않았으면 아무 일도 하지 않는다.</summary>
        public void Restore()
        {
            if (!_muted)
            {
                return;
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].forceRenderingOff = _wasOff[i];
                }
            }

            _muted = false;
        }
    }
}
