using System;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 나중에 생기는 협력자(예: 로봇이 준비된 뒤에 달리는 카메라)를 일정 간격으로 찾아 둔다. 미리 지정된 것이 있으면 그것을 쓰고,
    /// 없으면 찾을 때까지 간격마다 다시 찾는다. 한 번 찾으면 더 찾지 않는다. 찾는 방법과 시각은 주입받으므로 씬 없이 시험한다.
    /// </summary>
    public sealed class LazyComponentFinder<T> where T : class
    {
        private readonly Func<T> _find;
        private readonly float _intervalSeconds;
        private float _nextFindAt;

        /// <param name="preset">미리 지정된 것. 없으면 null.</param>
        /// <param name="intervalSeconds">다시 찾기까지 기다리는 시간(초).</param>
        /// <param name="find">씬에서 찾는 방법. 없으면 null을 돌려준다.</param>
        public LazyComponentFinder(T preset, float intervalSeconds, Func<T> find)
        {
            Found = preset;
            _intervalSeconds = intervalSeconds;
            _find = find ?? throw new ArgumentNullException(nameof(find));
        }

        /// <summary>찾은 것. 아직 못 찾았으면 null.</summary>
        public T Found { get; private set; }

        /// <summary>찾지 못했고 간격이 지났으면 한 번 찾아본다. 찾은 것이 있으면 true.</summary>
        public bool Refresh(float nowSeconds)
        {
            if (Found != null)
            {
                return true;
            }

            if (nowSeconds < _nextFindAt)
            {
                return false;
            }

            _nextFindAt = nowSeconds + _intervalSeconds;
            Found = _find();
            return Found != null;
        }
    }
}
