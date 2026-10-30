using System.Collections.Generic;

namespace MediKitTwin.Orders.Tests
{
    /// <summary>
    /// 테스트가 같은 조립을 반복하지 않도록 모은다. 실제 씬 연결 때도 같은 순서로 조립한다.
    /// 키트 코드와 수량 범위는 SRS FR-01의 값(EM, BL, DX / 1~10)이다.
    /// </summary>
    internal static class OrderTestFactory
    {
        internal static readonly string[] KitCodes = { "EM", "BL", "DX" };
        internal const int MinQuantity = 1;
        internal const int MaxQuantity = 10;
        internal const char CommentMarker = '#';

        internal static CsvLineSplitter Splitter() => new CsvLineSplitter(',', '"');

        internal static CsvOrderParser Parser(params IOrderRowValidator[] validators)
        {
            return new CsvOrderParser(Splitter(), CsvOrderColumns.Default, CommentMarker, validators);
        }

        internal static CsvOrderParser ParserWithDefaultRules()
        {
            return Parser(
                new KnownKitCodeValidator(KitCodes),
                new QuantityRangeValidator(MinQuantity, MaxQuantity),
                new UniqueSequenceValidator());
        }

        internal static IDefaultOrderProvider DefaultOrders()
        {
            return new PerKitCodeDefaultOrderProvider(KitCodes, "DEFAULT", 1);
        }

        internal static OrderIntake Intake(IOrderTextSource source)
        {
            var loader = new OrderLoader(source, ParserWithDefaultRules(), DefaultOrders());
            var planner = new KitRequestPlanner(new SequentialLotNumberGenerator("LOT-", 4));
            return new OrderIntake(loader, planner);
        }

        internal static List<string> Codes(IEnumerable<OrderIssue> issues)
        {
            var codes = new List<string>();
            foreach (OrderIssue issue in issues)
            {
                codes.Add(issue.Code);
            }

            return codes;
        }
    }
}
