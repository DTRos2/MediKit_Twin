using System;
using System.Collections.Generic;
using System.Linq;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 시드로 주문 CSV를 만든다. 정해 둔 주문(기본 주문 등)은 키트 순서가 늘 같아서, 키트 종류를 번갈아 만들거나 같은 종류를 반복했을 때만
    /// 생기는 문제(앞 키트가 남긴 팔 자세·베이스 위치, 같은 칸의 두 번째 물건)를 놓친다. 그래서 키트 코드와 수량을 섞어 여러 주문을 돌려 본다.
    /// 강화학습처럼 배우는 것이 아니라 입력을 흔들어 실패하는 조합을 찾는 시험이므로, 같은 시드면 항상 같은 주문이 나와야 한다.
    /// 키트 코드 목록은 주입받으므로 레시피를 늘려도 이 클래스는 고치지 않는다. 씬과 물리를 몰라서 씬 없이 시험한다.
    /// </summary>
    public sealed class RandomOrderGenerator
    {
        private const char ColumnSeparator = ',';
        private const char LineSeparator = '\n';

        private readonly string[] _kitCodes;
        private readonly string[] _hospitals;
        private readonly int _maxQuantityPerLine;

        /// <param name="kitCodes">주문에 쓸 수 있는 키트 코드. 순서와 대소문자, 중복은 상관없다(대문자로 맞춰 가나다순으로 늘어놓는다).</param>
        /// <param name="hospitals">병원명 후보. 키트 만들기에는 영향이 없지만 주문 줄의 한 칸이라 채워야 한다.</param>
        /// <param name="maxQuantityPerLine">주문 한 줄의 최대 수량. 작을수록 키트 종류가 자주 바뀐다.</param>
        public RandomOrderGenerator(IEnumerable<string> kitCodes, IReadOnlyList<string> hospitals, int maxQuantityPerLine)
        {
            if (kitCodes == null)
            {
                throw new ArgumentNullException(nameof(kitCodes));
            }

            // 집합에서 꺼내는 순서는 실행마다 다를 수 있으므로, 같은 시드가 같은 주문을 내도록 순서를 고정한다.
            _kitCodes = kitCodes
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code.Trim().ToUpperInvariant())
                .Distinct()
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToArray();

            if (_kitCodes.Length == 0)
            {
                throw new ArgumentException("키트 코드가 하나도 없다.", nameof(kitCodes));
            }

            if (hospitals == null || hospitals.Count == 0)
            {
                throw new ArgumentException("병원명이 하나도 없다.", nameof(hospitals));
            }

            if (maxQuantityPerLine < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxQuantityPerLine), "한 줄의 최대 수량은 1 이상이어야 한다.");
            }

            _hospitals = hospitals.ToArray();
            _maxQuantityPerLine = maxQuantityPerLine;
        }

        /// <param name="seed">같은 시드면 같은 주문이 나온다.</param>
        /// <param name="totalKits">주문 전체가 만들어 낼 키트 수. 줄마다 수량을 나눠 정확히 이 수가 되게 한다.</param>
        public GeneratedOrder Generate(int seed, int totalKits)
        {
            if (totalKits < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(totalKits), "키트는 한 개 이상이어야 한다.");
            }

            var random = new Random(seed);
            var lines = new List<string>();
            var summary = new List<string>();
            int remaining = totalKits;

            for (int sequence = 1; remaining > 0; sequence++)
            {
                string code = _kitCodes[random.Next(_kitCodes.Length)];
                string hospital = _hospitals[random.Next(_hospitals.Length)];
                // 남은 키트 수를 넘기지 않아야 전체 수가 맞는다. Next의 위쪽 끝은 포함되지 않는다.
                int quantity = random.Next(1, Math.Min(_maxQuantityPerLine, remaining) + 1);

                lines.Add(string.Join(ColumnSeparator.ToString(), sequence, hospital, code, quantity));
                summary.Add($"{code}×{quantity}");
                remaining -= quantity;
            }

            return new GeneratedOrder(seed, string.Join(LineSeparator.ToString(), lines), totalKits, string.Join(", ", summary));
        }
    }
}
