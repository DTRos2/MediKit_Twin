namespace MediKitTwin.Demo
{
    /// <summary>
    /// 키트 한 개가 공정 라인 어디까지 왔는지. CellEvents 신호가 오는 지점마다 하나씩이라,
    /// 신호가 없는 "키팅 중"은 값이 아니라 아직 키트 신호가 없는 상태(None)로 나타낸다.
    /// </summary>
    public enum ProcessStage
    {
        /// <summary>아직 컨베이어에 넘어온 키트가 없다.</summary>
        None,

        /// <summary>키팅을 마친 박스가 검사 구역으로 가는 중(KitReady).</summary>
        ToInspection,

        /// <summary>검사 구역에서 판정 중(AtInspection).</summary>
        Inspecting,

        /// <summary>불량으로 판정되어 푸셔가 밀어내는 중(InspectionResult 불합격).</summary>
        Rejecting,

        /// <summary>불량 박스에 넣었다(KitRejected). 이 키트의 길은 여기서 끝난다.</summary>
        Rejected,

        /// <summary>합격해서 팩킹 구역으로 가는 중(InspectionResult 합격).</summary>
        ToPacking,

        /// <summary>팩킹 구역에 서서 뚜껑을 덮는 중(AtPacking).</summary>
        Packing,

        /// <summary>뚜껑을 덮고 컨베이어 끝으로 가는 중(LidClosed).</summary>
        ToEnd,

        /// <summary>컨베이어 끝에 닿았거나 출하했다(AtConveyorEnd, KitShipped). 공정을 마쳤다.</summary>
        Done,
    }
}
