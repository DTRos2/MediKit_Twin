using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 컨베이어에 올릴 박스의 기준점을 계산한다. 컨베이어는 물건의 피벗을 벨트 위의 점(입력, 가운데, 출력)으로 옮기는데,
    /// 키트 박스의 피벗은 상자 한가운데라서 그대로 올리면 상자가 벨트에 반쯤 박힌다. 그래서 피벗이 바닥 중앙인 받침을 만들어 박스를 얹는다.
    /// </summary>
    public static class BoxPivot
    {
        /// <summary>
        /// 박스 바닥 중앙. 가로 위치는 박스 루트의 위치를 따르고(박스는 루트가 가운데에 있게 만들어져 있다),
        /// 높이는 박스를 감싸는 범위의 가장 낮은 곳이다. 구성품은 박스 안에 놓이므로 범위의 아래쪽을 바꾸지 않는다.
        /// </summary>
        public static Vector3 BottomCenter(Vector3 boxRootPosition, Bounds boxBounds)
        {
            return new Vector3(boxRootPosition.x, boxBounds.min.y, boxRootPosition.z);
        }
    }
}
