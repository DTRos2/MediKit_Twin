using System;
using MediKitTwin.Common;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 인식기를 감싸 결과를 알려 주는 쪽(예: 카메라 화면 표시)에 넘긴다. 어떤 인식기든 그대로 감쌀 수 있어서,
    /// 시뮬레이션 추정기와 실제 영상 인식기 모두 같은 방식으로 화면에 결과를 남긴다.
    /// </summary>
    public sealed class ObservationRecorder : IItemPoseEstimator
    {
        private readonly IItemPoseEstimator _inner;
        private readonly Action<ItemObservation> _sink;

        public ObservationRecorder(IItemPoseEstimator inner, Action<ItemObservation> sink)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public ItemObservation Observe(ShelfPickItem item)
        {
            ItemObservation observation = _inner.Observe(item);
            _sink(observation);
            return observation;
        }
    }
}
