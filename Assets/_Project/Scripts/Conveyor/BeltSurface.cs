using ConveyorSystem;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 컨베이어 벨트 윗면의 높이를 재고, 입력점이 그보다 얼마나 높은지 알려 준다.
    /// 컨베이어 에셋은 물건의 피벗을 입력점·가운데·출력점 높이에 두는데, 에셋의 기본 물건은 피벗이 한가운데라
    /// 그 높이가 곧 "벨트 윗면 + 물건 반높이"다. 우리 박스 받침은 피벗이 박스 바닥이라 같은 높이에 놓으면 그만큼 벨트 위에 뜬다.
    /// 그 차이를 값으로 박아 두지 않고 컨베이어마다 재서, 구간의 높이나 모양이 바뀌어도 박스가 벨트에 얹힌다.
    /// </summary>
    public static class BeltSurface
    {
        /// <summary>입력점이 벨트 윗면보다 높은 만큼(m). 입력점이 윗면보다 낮거나 같으면 0이다(박스를 올리지 않는다).</summary>
        public static float RestOffset(float inputPointY, float beltTopY)
        {
            return Mathf.Max(0f, inputPointY - beltTopY);
        }

        /// <summary>
        /// 이 구간의 입력점이 벨트 윗면보다 높은 만큼(m). 벨트 몸체가 구간 오브젝트의 렌더러라서 그 범위의 윗면을 벨트 윗면으로 본다.
        /// 렌더러가 없거나 입력점이 없으면 잴 수 없으므로 0이다.
        /// </summary>
        public static float InputAboveSurface(ConveyorBase conveyor)
        {
            if (conveyor == null || conveyor.InputPoint == null)
            {
                return 0f;
            }

            var body = conveyor.GetComponent<Renderer>();
            return body == null ? 0f : RestOffset(conveyor.InputPoint.position.y, body.bounds.max.y);
        }
    }
}
