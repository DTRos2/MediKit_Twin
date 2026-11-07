using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 카메라로 선반 물건 하나를 찾아본 결과. 찾았으면 추정한 물건 자세(월드), 화면 위치, 카메라에서의 거리를 담고,
    /// 못 찾았으면 이유(시야 밖, 가려짐)만 담는다.
    /// </summary>
    public readonly struct ItemObservation
    {
        private ItemObservation(ObservationStatus status, string slotId, Pose estimatedPose, Vector2 viewport, float distanceM)
        {
            Status = status;
            SlotId = slotId ?? string.Empty;
            EstimatedPose = estimatedPose;
            Viewport = viewport;
            DistanceM = distanceM;
        }

        public ObservationStatus Status { get; }

        /// <summary>찾아본 물건의 선반 칸 ID.</summary>
        public string SlotId { get; }

        /// <summary>추정한 물건 자세(월드). 찾지 못했으면 의미 없다.</summary>
        public Pose EstimatedPose { get; }

        /// <summary>화면 안 위치(0~1, 왼쪽 아래가 0,0). 찾았을 때만 의미 있다.</summary>
        public Vector2 Viewport { get; }

        /// <summary>카메라에서 물건까지 앞쪽으로 잰 거리(m).</summary>
        public float DistanceM { get; }

        public bool IsDetected => Status == ObservationStatus.Detected;

        public static ItemObservation None => new ItemObservation(ObservationStatus.NotAttempted, string.Empty, Identity, default, 0f);

        public static ItemObservation Seen(string slotId, Pose estimatedPose, Vector2 viewport, float distanceM)
        {
            return new ItemObservation(ObservationStatus.Detected, slotId, estimatedPose, viewport, distanceM);
        }

        public static ItemObservation Missed(string slotId, ObservationStatus status)
        {
            return new ItemObservation(status, slotId, Identity, default, 0f);
        }

        /// <summary>
        /// 찾은 물건이 카메라의 지금 자세에서 화면 어디에 보이는지 다시 계산한다. Viewport는 찾아본 순간의 값이라,
        /// 그 뒤로 로봇이 움직이면 화면의 표식이 물건에서 어긋난다. 화면에 표식을 그릴 때는 이것을 쓴다.
        /// 찾지 못한 결과이거나, 지금 자세에서 물건이 화면이나 거리 범위 밖이면 false다.
        /// </summary>
        public bool TryViewportFrom(PinholeCamera camera, Pose cameraPose, out Vector2 viewport)
        {
            viewport = default;
            return IsDetected && camera.TryProject(cameraPose, EstimatedPose.position, out viewport, out _);
        }

        /// <summary>사람이 읽는 한 줄 설명.</summary>
        public string Describe()
        {
            switch (Status)
            {
                case ObservationStatus.Detected:
                    return $"찾음 — 칸 {SlotId}, 거리 {DistanceM:0.00}m, 화면 ({Viewport.x:0.00}, {Viewport.y:0.00})";
                case ObservationStatus.OutOfView:
                    return $"시야 밖 — 칸 {SlotId} 물건이 카메라 화면이나 거리 범위를 벗어났다";
                case ObservationStatus.Occluded:
                    return $"가려짐 — 칸 {SlotId} 물건이 다른 물체에 가려 보이지 않는다";
                default:
                    return "아직 찾아보지 않았다";
            }
        }

        private static Pose Identity => new Pose(Vector3.zero, Quaternion.identity);
    }
}
