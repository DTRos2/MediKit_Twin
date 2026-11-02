namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 서는 자리를 고르는 방식의 설정. 미리 가늠할지, 미리 고른 자리 가운데 몇 곳까지 실제로 서 볼지만 담는다.
    /// 베이스를 못 옮기는 고정 팔이면 planInAdvance를 false로 준다.
    /// </summary>
    public readonly struct StandSearchSettings
    {
        public StandSearchSettings(bool planInAdvance, int maxStandAttempts)
        {
            PlanInAdvance = planInAdvance;
            MaxStandAttempts = maxStandAttempts < 1 ? 1 : maxStandAttempts;
        }

        /// <summary>이동하기 전에 서 볼 자리마다 팔이 닿는 방향이 있는지 가늠할지.</summary>
        public bool PlanInAdvance { get; }

        /// <summary>미리 고른 자리 가운데 실제로 서 보는 최대 횟수(1 이상).</summary>
        public int MaxStandAttempts { get; }
    }
}
