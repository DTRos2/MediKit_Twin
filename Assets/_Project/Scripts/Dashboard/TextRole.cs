namespace MediKitTwin.Dashboard
{
    /// <summary>글자의 쓰임새. 쓰임새마다 글꼴, 크기, 굵기가 정해져 있다(TextSpecs).</summary>
    public enum TextRole
    {
        /// <summary>패널 제목.</summary>
        Title,

        /// <summary>제목 옆의 영문 작은 라벨.</summary>
        Subtitle,

        /// <summary>본문.</summary>
        Body,

        /// <summary>굵은 본문(줄 제목, 병원 이름).</summary>
        BodyBold,

        /// <summary>작은 글(설명, 안내).</summary>
        Small,

        /// <summary>고정폭 숫자와 영문(시간, 각도).</summary>
        Mono,

        /// <summary>굵은 고정폭(태그, 관절 이름).</summary>
        MonoBold,

        /// <summary>큰 숫자(재고, 횟수).</summary>
        BigNumber,

        /// <summary>설비 상태 큰 글자.</summary>
        State,
    }
}
