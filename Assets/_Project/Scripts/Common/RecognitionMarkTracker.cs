namespace MediKitTwin.Common
{
    /// <summary>
    /// 물건을 인식한 결과 가운데 어느 것을 화면에 표시할지 정한다. 새로 찾았을 때부터 일정 시간만 보여 주고, 못 찾았다는 결과가 오면 바로 지운다.
    /// 마지막 결과는 다음 결과가 올 때까지 그대로 남아 있으므로, 같은 결과를 매 프레임 넘겨도 표시 시간이 다시 시작되지 않는다.
    /// 그리는 일과 시간 읽기는 모른다(지금 시각을 받는다).
    /// </summary>
    public sealed class RecognitionMarkTracker
    {
        private const float SamePositionSqrM = 1e-10f;

        private readonly float _showSeconds;
        private ItemObservation _current = ItemObservation.None;
        private bool _hasMark;
        private float _shownAt;

        /// <param name="showSeconds">찾은 뒤 표시를 보여 주는 시간(초).</param>
        public RecognitionMarkTracker(float showSeconds)
        {
            _showSeconds = showSeconds;
        }

        /// <summary>가장 최근 인식 결과를 알려 준다. 매 프레임 불러도 된다.</summary>
        public void Observe(ItemObservation observation, float nowSeconds)
        {
            if (!observation.IsDetected)
            {
                _hasMark = false;
                return;
            }

            if (_hasMark && IsSame(_current, observation))
            {
                return;
            }

            _current = observation;
            _hasMark = true;
            _shownAt = nowSeconds;
        }

        /// <summary>지금 표시할 인식 결과가 있으면 true.</summary>
        public bool TryGetMark(float nowSeconds, out ItemObservation mark)
        {
            mark = _current;
            return _hasMark && nowSeconds - _shownAt <= _showSeconds;
        }

        private static bool IsSame(ItemObservation a, ItemObservation b)
        {
            return a.SlotId == b.SlotId
                && (a.EstimatedPose.position - b.EstimatedPose.position).sqrMagnitude < SamePositionSqrM;
        }
    }
}
