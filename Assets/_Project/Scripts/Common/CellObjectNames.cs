namespace MediKitTwin.Common
{
    /// <summary>
    /// Scene_Cell에서 코드와 에디터 도구와 시험이 이름으로 찾는 오브젝트의 이름. 씬을 만드는 쪽과 찾는 쪽이 같은 값을 쓰도록 한곳에 둔다.
    /// 한 파일에서만 쓰는 이름은 그 파일에 둔다.
    /// </summary>
    public static class CellObjectNames
    {
        public const string Managers = "Managers";
        public const string PackingSensor = "Sensor_Packing";
        public const string ConveyorEndSensor = "Sensor_ConveyorEnd";

        /// <summary>팩킹 센서 아래에서 박스가 멈출 자리(팔 B가 맞춰진 자리).</summary>
        public const string PackingStopPoint = "PackingStopPoint";

        /// <summary>뚜껑 이름의 머리말(Lid_01, Lid_02, ...). 맨 위가 Lid_01이다.</summary>
        public const string LidPrefix = "Lid_";

        /// <summary>
        /// 키트 박스로 보는 오브젝트 이름에 들어 있어야 하는 글자. 팔 B의 뚜껑 덮기가 이 글자가 이름에 든 첫 자손을 박스로 쓰므로,
        /// 박스가 아닌 오브젝트(컨베이어의 박스 받침 등)의 이름에는 이 글자를 넣지 않는다.
        /// </summary>
        public const string KitBoxKey = "KitBox";
    }
}
