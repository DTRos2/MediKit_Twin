namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 카메라로 물건을 본 뒤 집는 자리를 고친 결과. 고쳤는지(IsCorrected), 인식이 꼭 필요한데 못 해서 멈춰야 하는지(IsRejected)와,
    /// 이후 쥐고 뺄 때 쓸 자리(Targets)를 담는다.
    /// </summary>
    public readonly struct GraspRefinement
    {
        private GraspRefinement(GraspTargets targets, bool isCorrected, bool isRejected, string note)
        {
            Targets = targets;
            IsCorrected = isCorrected;
            IsRejected = isRejected;
            Note = note ?? string.Empty;
        }

        public GraspTargets Targets { get; }

        /// <summary>계획한 자리를 카메라로 본 위치에 맞춰 고쳤으면 true.</summary>
        public bool IsCorrected { get; }

        /// <summary>인식이 필수인데 물건을 못 찾아 집지 말아야 하면 true.</summary>
        public bool IsRejected { get; }

        /// <summary>고치거나 고치지 않은 이유, 또는 중단한 이유.</summary>
        public string Note { get; }

        /// <summary>계획한 자리를 그대로 쓴다.</summary>
        public static GraspRefinement Keep(GraspTargets planned, string note)
        {
            return new GraspRefinement(planned, false, false, note);
        }

        public static GraspRefinement Corrected(GraspTargets corrected, string note)
        {
            return new GraspRefinement(corrected, true, false, note);
        }

        public static GraspRefinement Rejected(GraspTargets planned, string note)
        {
            return new GraspRefinement(planned, false, true, note);
        }
    }
}
