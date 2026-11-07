namespace MediKitTwin.Dashboard
{
    /// <summary>버튼의 종류.</summary>
    public enum ButtonKind
    {
        /// <summary>보통 버튼(옅은 바탕, 테두리).</summary>
        Secondary,

        /// <summary>가장 중요한 버튼(강조색 바탕, 흰 글자).</summary>
        Primary,

        /// <summary>켜진 상태의 토글 버튼(옅은 강조색 바탕, 강조색 글자).</summary>
        Selected,
    }
}
