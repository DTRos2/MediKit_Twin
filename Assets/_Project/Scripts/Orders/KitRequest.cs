using MediKitTwin.Common;

namespace MediKitTwin.Orders
{
    /// <summary>
    /// 아직 셀에 투입되지 않은 키트 1개분의 요청. 주문 정보와 로트번호는 주문을 읽을 때 정해지지만
    /// KitSequence는 로봇암 A가 가져갈 때(투입 순서) 정해지므로, 그때까지는 KitData가 아닌 이 형태로 둔다.
    /// 재고 소진으로 보류된 키트를 재개할 때도 같은 요청으로 새 KitData를 만든다.
    /// </summary>
    public sealed class KitRequest
    {
        public KitRequest(int orderSequence, string hospitalName, string kitCode, string lotNumber)
        {
            OrderSequence = orderSequence;
            HospitalName = hospitalName;
            KitCode = kitCode;
            LotNumber = lotNumber;
        }

        public int OrderSequence { get; }

        public string HospitalName { get; }

        public string KitCode { get; }

        public string LotNumber { get; }

        /// <summary>투입 순서를 받아 새 KitData를 만든다. 호출할 때마다 누락 정답과 감지 결과가 비어 있는 새 객체이다.</summary>
        public KitData CreateKitData(int kitSequence)
        {
            return new KitData(kitSequence, OrderSequence, LotNumber, HospitalName, KitCode);
        }
    }
}
