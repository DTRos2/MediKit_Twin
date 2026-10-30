namespace MediKitTwin.Orders
{
    /// <summary>
    /// 주문 파일을 읽는 규칙의 설정 값. 수량 범위, CSV 모양, 기본 주문, 로트번호 형식은 코드에 쓰지 않고
    /// 호출하는 쪽(Inspector, SO)이 정해 넘긴다. (FR-01)
    /// </summary>
    public sealed class OrderIntakeSettings
    {
        public OrderIntakeSettings(
            int minQuantity,
            int maxQuantity,
            string defaultHospitalName,
            int defaultQuantity,
            string lotPrefix,
            int lotDigits,
            char delimiter,
            char quote,
            char commentMarker,
            CsvOrderColumns columns)
        {
            MinQuantity = minQuantity;
            MaxQuantity = maxQuantity;
            DefaultHospitalName = defaultHospitalName;
            DefaultQuantity = defaultQuantity;
            LotPrefix = lotPrefix;
            LotDigits = lotDigits;
            Delimiter = delimiter;
            Quote = quote;
            CommentMarker = commentMarker;
            Columns = columns;
        }

        public int MinQuantity { get; }

        public int MaxQuantity { get; }

        /// <summary>주문 파일이 없을 때 쓰는 기본 주문의 병원명.</summary>
        public string DefaultHospitalName { get; }

        /// <summary>기본 주문에서 키트 종류마다 만들 수량.</summary>
        public int DefaultQuantity { get; }

        public string LotPrefix { get; }

        public int LotDigits { get; }

        public char Delimiter { get; }

        public char Quote { get; }

        /// <summary>이 문자로 시작하는 줄은 주석으로 보고 건너뛴다.</summary>
        public char CommentMarker { get; }

        public CsvOrderColumns Columns { get; }
    }
}
