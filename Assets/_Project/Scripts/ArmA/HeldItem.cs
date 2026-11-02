using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 손에 쥐고 있는 물건의 지금 모습. 놓을 때 물건이 어떤 자세로 박스에 닿을지 미리 계산하는 데 쓴다.
    /// </summary>
    public readonly struct HeldItem
    {
        public HeldItem(Vector3 center, Quaternion rotation, Quaternion restingRotation)
        {
            Center = center;
            Rotation = rotation;
            RestingRotation = restingRotation;
        }

        /// <summary>물건 충돌체가 감싸는 영역의 중심(월드). 슬롯 자리에 맞추는 기준이다.</summary>
        public Vector3 Center { get; }

        /// <summary>물건의 지금 회전(월드).</summary>
        public Quaternion Rotation { get; }

        /// <summary>
        /// 물건이 선반에 놓여 있던 회전(월드). 선반 위에 얹혀 있던 자세이므로 물건이 스스로 안정되게 놓이는 방향이다.
        /// 박스에 놓을 때도 이 방향으로 놓으면 물건이 모서리로 서지 않고 눕거나 원래대로 선다.
        /// </summary>
        public Quaternion RestingRotation { get; }
    }
}
