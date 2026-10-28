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

        /// <summary>정상이면 true, 누락이면 false.</summary>
        public bool IsPass { get; }

        /// <summary>
        /// 비어 있던 슬롯 번호(레시피의 슬롯 번호). 규칙 판정만 채운다.
        /// CNN은 정상·누락 2클래스 분류라 위치를 알 수 없으므로 null이다. 빈 목록은 "누락 슬롯 없음"이다.
        /// </summary>
        public IReadOnlyList<int> MissingSlots { get; }

        /// <summary>판정 확신도(0~1). 규칙 판정은 항상 1이다.</summary>
        public float Confidence { get; }

        public JudgeMethod Method { get; }
    }
}
