using System;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 시뮬레이션용 물건 인식기. 영상을 분석하는 것이 아니라, 월드에서 읽은 물건의 실제 자세를 카메라가 볼 수 있는지로 걸러 센서처럼 내놓는다.
    ///  1) 물건이 카메라 시야(화면, 최소·최대 거리) 안에 있어야 한다.
    ///  2) 카메라에서 물건까지 다른 물체에 가려지지 않아야 한다.
    ///  3) 찾았으면 위치에 센서 잡음을 섞는다. 방향은 실제 그대로다.
    /// 실제 로봇에서는 이 자리에 영상 인식기(IItemPoseEstimator의 다른 구현)가 들어간다. 카메라 구현과 월드 조회와 가림 검사는 주입받아 씬 없이 시험한다.
    /// </summary>
    public sealed class SimulatedItemPoseEstimator : IItemPoseEstimator
    {
        private readonly IToolCameraView _camera;
        private readonly Func<ShelfPickItem, Pose> _actualPose;
        private readonly Func<Vector3, Vector3, bool> _isBlocked;
        private readonly ObservationNoise _noise;

        /// <param name="actualPose">물건의 지금 실제 자세(월드)를 읽는 함수. 센서가 월드를 보는 것에 해당한다.</param>
        /// <param name="isBlocked">(카메라 위치, 물건 위치) 사이가 가려지면 true. 가림을 따지지 않으려면 null.</param>
        /// <param name="noise">위치에 섞을 잡음. null이면 잡음이 없다.</param>
        public SimulatedItemPoseEstimator(
            IToolCameraView camera,
            Func<ShelfPickItem, Pose> actualPose,
            Func<Vector3, Vector3, bool> isBlocked,
            ObservationNoise noise)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _actualPose = actualPose ?? throw new ArgumentNullException(nameof(actualPose));
            _isBlocked = isBlocked;
            _noise = noise ?? ObservationNoise.None;
        }

        public ItemObservation Observe(ShelfPickItem item)
        {
            if (!_camera.IsReady)
            {
                return ItemObservation.Missed(item.SlotId, ObservationStatus.NotAttempted);
            }

            Pose actual = _actualPose(item);
            Pose cameraPose = _camera.CameraPose;

            if (!_camera.Model.TryProject(cameraPose, actual.position, out Vector2 viewport, out float depthM))
            {
                return ItemObservation.Missed(item.SlotId, ObservationStatus.OutOfView);
            }

            if (_isBlocked != null && _isBlocked(cameraPose.position, actual.position))
            {
                return ItemObservation.Missed(item.SlotId, ObservationStatus.Occluded);
            }

            var estimated = new Pose(actual.position + _noise.SampleOffset(), actual.rotation);
            return ItemObservation.Seen(item.SlotId, estimated, viewport, depthM);
        }
    }
}
