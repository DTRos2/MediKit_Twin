using System.Collections.Generic;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 검사 판정 결과. 규칙 판정(FR-06)과 ML 판정(FR-07)이 같은 형식을 쓴다.
    /// 값이 없는 default 상태가 "불합격"으로 읽히지 않도록, 판정 전에는 KitData.Verdict를 null로 둔다.
    /// </summary>
    public readonly struct InspectionVerdict
    {
        public InspectionVerdict(bool isPass, IReadOnlyList<int> missingSlots, float confidence, JudgeMethod method)
        {
            IsPass = isPass;
            // 호출한 쪽이 리스트를 나중에 바꿔도 판정 결과가 변하지 않도록 복사해 둔다.
            MissingSlots = missingSlots == null ? null : new List<int>(missingSlots).AsReadOnly();
            Confidence = confidence;
            Method = method;
        }

        /// <summary>정상이면 true, 누락이나 오구성이 있으면 false.</summary>
        public bool IsPass { get; }

        /// <summary>
        /// 레시피와 맞지 않는 슬롯 번호(비어 있거나 다른 구성품이 든 슬롯). 규칙 판정과 CNN 판정(슬롯별 분류)이 모두 채운다.
        /// 슬롯별로 판정할 수 없으면 null이다. 규칙 판정에서 슬롯 센서가 응답하지 않을 때(FR-06 예외)와
        /// CNN이 슬롯 크롭을 만들지 못할 때가 그렇다. 빈 목록은 "불일치 슬롯 없음"이며 IsPass가 true일 때만 쓴다.
        /// </summary>
        public IReadOnlyList<int> MissingSlots { get; }

        /// <summary>판정 확신도(0~1). 규칙 판정은 항상 1이다.</summary>
        public float Confidence { get; }

        public JudgeMethod Method { get; }
    }
}
