using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>박스 슬롯에 놓는 동작의 높이 설정과, 스스로 찾을 때 도구 방향이 기본에서 벗어날 수 있는 범위.</summary>
    public readonly struct BoxPlaceSettings
    {
        /// <param name="restingSearch">
        /// 물건이 선반에서 놓여 있던 방향 그대로 박스에 놓이도록 도구 방향을 잡을 때, 그 방향에서 벗어날 수 있는 범위.
        /// null이면 이 방식을 쓰지 않고 항상 위에서 아래로 내려놓는다.
        /// </param>
        /// <param name="releaseHeights">
        /// 슬롯 중심보다 이만큼 위에서 놓고 물건을 떨어뜨리는 높이 후보들. 낮은 것부터 시도해 닿는 가장 낮은 높이로 놓는다.
        /// null이면 슬롯 중심에서 놓는다.
        /// </param>
        public BoxPlaceSettings(
            float approachHeightM, float liftHeightM, Vector3 gripYawForward, OrientationSearchSettings search,
            OrientationSearchSettings restingSearch = null, ReleaseHeights releaseHeights = null)
        {
            ApproachHeightM = approachHeightM;
            LiftHeightM = liftHeightM;
            GripYawForward = gripYawForward;
            Search = search;
            RestingSearch = restingSearch;
            _releaseHeights = releaseHeights;
        }

        private readonly ReleaseHeights _releaseHeights;

        /// <summary>
        /// 슬롯 중심보다 위에서 물건을 놓는 높이 후보들(m), 낮은 것부터. 좁은 박스에서는 벌린 손가락이 벽과 이웃 물건 사이로 들어가지 못할 수 있어,
        /// 슬롯마다 닿는 가장 낮은 높이를 고르려는 사다리다. 정하지 않았으면 슬롯 중심(0) 하나뿐이다.
        /// </summary>
        public ReleaseHeights ReleaseHeights => _releaseHeights ?? ReleaseHeights.AtSlotCenter;

        /// <summary>
        /// 물건을 선반에서 놓여 있던 방향 그대로 놓을 때 도구 방향이 벗어날 수 있는 범위. 없으면 null.
        /// 손목 비틀기는 물건을 가로로 눕히므로, 호출하는 쪽이 180°씩만 비틀게(뒤집어 놓는 것은 허용) 정해 넘긴다.
        /// </summary>
        public OrientationSearchSettings RestingSearch { get; }

        /// <summary>선반에서 놓여 있던 방향으로 놓는 방식을 쓰면 true.</summary>
        public bool PlacesInRestingOrientation => RestingSearch != null;

        /// <summary>박스 슬롯 위에서 내려가기 시작하는 높이(m).</summary>
        public float ApproachHeightM { get; }

        /// <summary>물건을 놓은 뒤 들어올리는 높이(m).</summary>
        public float LiftHeightM { get; }

        /// <summary>도구가 아래를 볼 때 위쪽이 되는 수평 방향. 손가락이 벌어지는 방향을 정하는 기준이다.</summary>
        public Vector3 GripYawForward { get; }

        public OrientationSearchSettings Search { get; }

        /// <summary>기본 도구 방향: 똑바로 아래를 보고, 손가락 벌어짐은 GripYawForward 기준이다.</summary>
        public Quaternion NominalToolRotation()
        {
            Vector3 forward = Vector3.ProjectOnPlane(GripYawForward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f)
            {
                forward = Vector3.forward;
            }

            return Quaternion.LookRotation(Vector3.down, forward.normalized);
        }
    }
}
