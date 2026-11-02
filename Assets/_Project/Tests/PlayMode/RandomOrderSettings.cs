using System;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 랜덤 주문 시험이 쓰는 시드와 키트 수. 시험 코드를 고치지 않고 여러 시드를 돌릴 수 있게 환경 변수로 받는다.
    /// 잘못된 값이나 빈 값이면 기본값을 쓰므로, 아무것도 정하지 않아도 같은 시험이 같은 결과를 낸다.
    /// </summary>
    public readonly struct RandomOrderSettings
    {
        public const string SeedVariable = "MEDIKIT_E2E_SEED";
        public const string KitsVariable = "MEDIKIT_E2E_KITS";

        public const int DefaultSeed = 1;

        // 키트 하나가 시뮬레이션으로 약 80초라, 다섯 개면 약 7분(실제 약 4분)이다.
        public const int DefaultKits = 5;

        // 너무 많이 넣으면 한 시험이 몇 시간이 된다. 더 길게 보고 싶으면 시드를 바꿔 여러 번 돌린다.
        public const int MaxKits = 20;

        public RandomOrderSettings(int seed, int kits)
        {
            Seed = seed;
            Kits = kits;
        }

        public int Seed { get; }

        public int Kits { get; }

        public static RandomOrderSettings FromEnvironment()
        {
            return Parse(Environment.GetEnvironmentVariable(SeedVariable), Environment.GetEnvironmentVariable(KitsVariable));
        }

        /// <param name="seedText">시드 글. 어떤 정수든 시드가 된다. 비었거나 정수가 아니면 기본 시드.</param>
        /// <param name="kitsText">키트 수 글. 1~MaxKits로 맞춘다. 비었거나 정수가 아니면 기본 키트 수.</param>
        public static RandomOrderSettings Parse(string seedText, string kitsText)
        {
            int seed = int.TryParse(seedText, out int parsedSeed) ? parsedSeed : DefaultSeed;
            int kits = int.TryParse(kitsText, out int parsedKits) ? Math.Max(1, Math.Min(MaxKits, parsedKits)) : DefaultKits;
            return new RandomOrderSettings(seed, kits);
        }
    }
}
