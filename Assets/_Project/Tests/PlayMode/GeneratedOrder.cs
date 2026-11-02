namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 랜덤으로 만든 주문 한 건. 실패했을 때 같은 시드로 같은 주문을 다시 돌릴 수 있도록 시드와 주문 글을 함께 들고 있다.
    /// </summary>
    public sealed class GeneratedOrder
    {
        public GeneratedOrder(int seed, string csv, int kitCount, string summary)
        {
            Seed = seed;
            Csv = csv;
            KitCount = kitCount;
            Summary = summary;
        }

        public int Seed { get; }

        /// <summary>주문 입력(IOrderInput.LoadText)에 그대로 넣는 CSV 글. 한 줄이 순번, 병원명, 키트 코드, 수량이다.</summary>
        public string Csv { get; }

        /// <summary>이 주문이 만들어 내야 하는 키트의 총 수(수량의 합).</summary>
        public int KitCount { get; }

        /// <summary>"EM×1, DX×2"처럼 사람이 한눈에 보는 요약. 같은 코드가 이어지면 따로 적어 키트 순서가 보이게 한다.</summary>
        public string Summary { get; }

        /// <summary>실패 메시지 앞에 붙이는 설명. 이것만 보고도 같은 시험을 다시 돌릴 수 있어야 한다.</summary>
        public string Describe()
        {
            return $"랜덤 주문(시드 {Seed}, 키트 {KitCount}개): {Summary}";
        }
    }
}
