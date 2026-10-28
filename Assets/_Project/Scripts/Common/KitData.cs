namespace MediKitTwin.Common
{
    /// <summary>
    /// 키트 1개(박스 1개)의 데이터. 누락, 검사, 출하는 키트 단위로 처리한다.
    /// 주문 정보(OrderSequence, HospitalName)는 보류와 재개를 주문 단위로 처리하기 위해 함께 가진다.
    /// 시각은 모두 시뮬레이션 시간(초)이다.
    /// </summary>
    public sealed class KitData
    {
        private int? _missingAnswer;

        public KitData(int kitSequence, int orderSequence, string lotNumber, string hospitalName, string kitCode)
        {
            KitSequence = kitSequence;
            OrderSequence = orderSequence;
            LotNumber = lotNumber;
            HospitalName = hospitalName;
            // 레시피의 키트 코드(대문자)와 조회가 어긋나지 않도록 대문자로 맞춘다.
            KitCode = kitCode?.Trim().ToUpperInvariant();
        }

        /// <summary>셀에 투입되는 순서(1, 2, 3, ...). 실행 안에서 유일하다.</summary>
        public int KitSequence { get; }

        public int OrderSequence { get; }

        /// <summary>실행 안에서 유일한 식별 번호. 라벨에 병원명과 함께 표시한다.</summary>
        public string LotNumber { get; }

        public string HospitalName { get; }

        /// <summary>키트 종류 코드(EM, BL, DX). 레시피의 KitCode와 같은 문자열이며 대문자로 쓴다. enum이 아닌 이유는 레시피 파일만 추가해 키트 종류를 늘리기 위해서다. (NFR-07)</summary>
        public string KitCode { get; }

        /// <summary>
        /// 슬롯 감지 센서가 만든 박스 내용. 인덱스는 레시피의 슬롯 번호 - 1이고 값은 감지된 구성품 ID이며,
        /// 비어 있는 슬롯은 null이다. 길이는 레시피의 슬롯 수와 같다. 검사 전에는 배열 자체가 null이다.
        /// </summary>
        public string[] DetectedComponentIds { get; set; }

        /// <summary>
        /// 판정 결과. 판정 전에는 null이다. 판정 결과는 이 필드가 유일한 출처이며,
        /// 검사 쪽은 이 값을 채운 뒤에 CellEvents.InspectionResult를 발행한다.
        /// </summary>
        public InspectionVerdict? Verdict { get; set; }

        /// <summary>피킹을 시작한 시각. 시작 전에는 null이다.</summary>
        public float? StartTime { get; set; }

        /// <summary>하역을 마치거나 불량 박스에 도달한 시각. 끝나기 전에는 null이다.</summary>
        public float? CompleteTime { get; set; }

        /// <summary>누락 규칙(FR-04)이 빠뜨린 구성품의 슬롯 번호를 기록한다. 누락이 아니면 호출하지 않는다.</summary>
        public void SetMissingAnswer(int slotNumber)
        {
            _missingAnswer = slotNumber;
        }

        /// <summary>
        /// 누락 정답(빠진 슬롯 번호, 누락이 아니면 null). 판정의 MissingSlots와 같은 번호 체계이며 평가용 로그 비교에만 쓴다.
        /// 검사 판정 코드(FR-06, FR-07)는 이 메서드를 호출하지 않는다.
        /// </summary>
        public int? GetMissingAnswerForEvaluation()
        {
            return _missingAnswer;
        }
    }
}
