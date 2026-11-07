namespace MediKitTwin.ArmA
{
    /// <summary>도구 끝 카메라를 어디에 달지. 좌우는 도구 끝점(TCP) 기준이다(오른쪽이 +X).</summary>
    public enum ToolCameraMount
    {
        /// <summary>손 몸통의 오른쪽(+X) 옆면에 달고, 도구 끝점 앞쪽 가운데를 본다.</summary>
        HandRightSide = 0,

        /// <summary>손 몸통의 왼쪽(−X) 옆면에 달고, 도구 끝점 앞쪽 가운데를 본다.</summary>
        HandLeftSide = 1,

        /// <summary>설정한 위치와 방향 그대로 단다.</summary>
        Manual = 2,

        /// <summary>그리퍼 가운데(손 몸통 옆·위아래 한가운데)의 앞끝에 달고, 도구 끝점과 같은 쪽(앞)을 똑바로 본다.</summary>
        HandCenter = 3
    }
}
