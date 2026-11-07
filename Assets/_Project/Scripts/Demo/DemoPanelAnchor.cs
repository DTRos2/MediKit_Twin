namespace MediKitTwin.Demo
{
    /// <summary>
    /// 패널이 붙는 화면 모서리. 패널 위치는 이 모서리에서 떨어진 거리(여백)로 정한다.
    /// BottomCenter는 아래쪽 가운데이며, 가로 여백은 가운데에서 오른쪽으로 옮기는 거리다(0이면 정확히 가운데).
    /// 저장된 숫자가 바뀌지 않도록 새 값은 맨 끝에만 더한다.
    /// </summary>
    public enum DemoPanelAnchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        BottomCenter
    }
}
