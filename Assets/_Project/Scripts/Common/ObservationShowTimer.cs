namespace MediKitTwin.Common
{
    /// <summary>
    /// 물건을 찾아본 횟수가 늘면 그때부터 일정 시간만 화면을 보여 주기로 정한다. 찾았는지 못 찾았는지는 가리지 않는다(못 찾은 이유도 화면에서 봐야 하므로).
    /// 같은 횟수를 매 프레임 넘겨도 표시 시간이 다시 시작되지 않는다. 그리는 일과 시간 읽기는 모른다(지금 시각을 받는다).
    /// </summary>
    public sealed class ObservationShowTimer
    {
        private readonly float _showSeconds;
        private int _lastCount;
        private float _shownAt = float.NegativeInfinity;

        /// <param name="showSeconds">찾아본 뒤 화면을 보여 주는 시간(초).</param>
        public ObservationShowTimer(float showSeconds)
        {
            _showSeconds = showSeconds;
        }

        /// <summary>지금까지 찾아본 횟수를 알려 준다. 매 프레임 불러도 된다. 횟수가 바뀐 순간부터 표시 시간을 센다.</summary>
        public void Observe(int observationCount, float nowSeconds)
        {
            if (observationCount == _lastCount)
            {
                return;
            }

            _lastCount = observationCount;
            _shownAt = nowSeconds;
        }

        /// <summary>지금 화면을 보여 줘야 하면 true.</summary>
        public bool IsVisible(float nowSeconds)
        {
            return nowSeconds - _shownAt <= _showSeconds;
        }
    }
}
