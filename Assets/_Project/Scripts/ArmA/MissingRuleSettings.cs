using MediKitTwin.Common;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 누락 발생 규칙의 설정 값. (FR-04) 값은 코드에 쓰지 않고 시나리오 설정(ScenarioConfigSO)에서 읽어 넘긴다.
    /// </summary>
    public sealed class MissingRuleSettings
    {
        public MissingRuleSettings(float probabilityPercent, int fixedKitSequence, int fixedSlotNumber, int seed)
        {
            ProbabilityPercent = probabilityPercent;
            FixedKitSequence = fixedKitSequence;
            FixedSlotNumber = fixedSlotNumber;
            Seed = seed;
        }

        /// <summary>시나리오 설정(ScenarioConfigSO)의 누락 값으로 만든다. 값을 읽어 옮기는 일을 호출하는 쪽마다 반복하지 않게 한 곳에 둔다.</summary>
        public static MissingRuleSettings From(ScenarioConfigSO scenario)
        {
            return new MissingRuleSettings(
                scenario.MissingProbabilityPercent,
                scenario.FixedMissingKitSequence,
                scenario.FixedMissingSlotNumber,
                scenario.RandomSeed);
        }

        /// <summary>키트마다 누락이 발생할 확률(%). 0~100 밖의 값은 가까운 끝값으로 본다.</summary>
        public float ProbabilityPercent { get; }

        /// <summary>항상 누락시킬 키트 순번. 0이면 사용하지 않는다.</summary>
        public int FixedKitSequence { get; }

        /// <summary>고정 누락 키트에서 빠뜨릴 슬롯 번호. 0이면 무작위로 고른다.</summary>
        public int FixedSlotNumber { get; }

        /// <summary>난수 시드. 같은 시드와 같은 키트 순번이면 같은 결과가 나온다. (NFR-06)</summary>
        public int Seed { get; }
    }
}
