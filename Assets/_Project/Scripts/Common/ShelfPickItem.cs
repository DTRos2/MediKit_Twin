using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 선반 칸에서 지금 집을 수 있는 물건 하나. 집을 자리(월드 자세), 칸에서 빠져나오는 방향, 로봇암이 쥔 물건으로 복제할 원본을 담는다.
    /// 선반 화면은 재고가 줄면 원본을 숨기므로, 쥐고 가는 물건은 원본이 아니라 그 복제본이다.
    /// </summary>
    public readonly struct ShelfPickItem
    {
        public ShelfPickItem(Pose pose, Vector3 approachDirection, GameObject prototype)
        {
            Pose = pose;
            ApproachDirection = approachDirection;
            Prototype = prototype;
        }

        public Pose Pose { get; }

        /// <summary>칸의 열린 쪽을 향한 월드 방향(칸 밖으로 나가는 방향). 칸은 위가 막혀 있어 로봇은 이 방향에서 수평으로 들어가야 한다.</summary>
        public Vector3 ApproachDirection { get; }

        public GameObject Prototype { get; }
    }
}
