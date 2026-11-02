namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔 동작이 "닿았다"고 보는 허용 오차. 지나가는 자리(접근, 후퇴, 들어올리기)는 느슨하게, 쥐고 놓는 자리는 엄격하게 판정한다.
    /// 도구 끝점 목표는 위치(m)와 회전(도), 관절 각도 목표는 관절 각도 오차(도)로 판정한다.
    /// 지나가는 자리는 "닿았다"뿐 아니라 "IK가 풀렸다"는 기준도 느슨하게 줄 수 있다(TravelSolve…). 쥐고 놓는 자리는 늘 엄격하다.
    /// </summary>
    public readonly struct MotionTolerances
    {
        /// <param name="travelSolvePositionM">지나가는 자리에서 IK가 수렴하지 못해도 풀린 것으로 보는 위치 오차(m). 0이면 엄격하다.</param>
        /// <param name="travelSolveRotationDeg">지나가는 자리에서 IK가 수렴하지 못해도 풀린 것으로 보는 회전 오차(도). 0이면 엄격하다.</param>
        public MotionTolerances(
            float travelPositionM, float travelRotationDeg, float precisePositionM, float preciseRotationDeg,
            float travelJointDeg, float preciseJointDeg, float travelSolvePositionM = 0f, float travelSolveRotationDeg = 0f)
        {
            TravelPositionM = travelPositionM;
            TravelRotationDeg = travelRotationDeg;
            PrecisePositionM = precisePositionM;
            PreciseRotationDeg = preciseRotationDeg;
            TravelJointDeg = travelJointDeg;
            PreciseJointDeg = preciseJointDeg;
            TravelSolvePositionM = travelSolvePositionM;
            TravelSolveRotationDeg = travelSolveRotationDeg;
        }

        public float TravelSolvePositionM { get; }

        public float TravelSolveRotationDeg { get; }

        public float TravelPositionM { get; }

        public float TravelRotationDeg { get; }

        public float PrecisePositionM { get; }

        public float PreciseRotationDeg { get; }

        public float TravelJointDeg { get; }

        public float PreciseJointDeg { get; }
    }
}
