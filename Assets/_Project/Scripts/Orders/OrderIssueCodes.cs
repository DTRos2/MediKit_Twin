namespace MediKitTwin.Orders
{
    /// <summary>주문 불러오기 경고 코드. 대시보드와 로거가 코드로 경고 종류를 구분한다.</summary>
    public static class OrderIssueCodes
    {
        public const string FileMissing = "ORDER_FILE_MISSING";
        public const string InvalidRow = "ORDER_INVALID_ROW";
        public const string UnknownKitCode = "ORDER_UNKNOWN_KIT_CODE";
        public const string QuantityOutOfRange = "ORDER_QUANTITY_OUT_OF_RANGE";
        public const string DuplicateSequence = "ORDER_DUPLICATE_SEQUENCE";
        public const string NoValidOrder = "ORDER_NO_VALID_ORDER";
        public const string RecipeMissing = "ORDER_RECIPE_MISSING";
        public const string RepeatedHold = "ORDER_REPEATED_HOLD";
    }
}
