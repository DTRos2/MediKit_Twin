using MediKitTwin.Common;

namespace MediKitTwin.Label
{
    /// <summary>
    /// 라벨에 적을 글. 키트의 병원명, 로트 번호, 키트 코드를 한 줄씩 낸다. (FR-11)
    /// 라벨과 표시판이 같은 글을 쓰도록 만드는 곳을 하나로 둔다. 값이 비어 있으면 줄이 비지 않게 "-"를 적는다.
    /// </summary>
    public static class LabelText
    {
        public const int LineCount = 3;

        private const string Empty = "-";

        public static string[] Compose(KitData kit)
        {
            return new[]
            {
                OrEmpty(kit?.HospitalName),
                OrEmpty(kit?.LotNumber),
                OrEmpty(kit?.KitCode)
            };
        }

        /// <summary>여러 줄을 한 글로 잇는다(TextMesh에 그대로 넣는다).</summary>
        public static string Join(string[] lines)
        {
            return string.Join("\n", lines);
        }

        private static string OrEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? Empty : value;
        }
    }
}
