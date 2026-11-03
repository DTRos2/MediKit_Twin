using System;
using UnityEngine;

namespace MediKitTwin.ML
{
    /// <summary>
    /// 박스 하나를 촬영할 때 쓰는 연속 값(위치 오차, 박스 회전, 조명, 색조). 박스 설계의 시드에서 정해지므로
    /// 같은 시드와 같은 범위는 항상 같은 값을 낸다. (NFR-06)
    /// 위치는 박스 기준 로컬 좌표의 XZ 평면 값이며, 높이는 촬영하는 쪽이 정한다.
    /// </summary>
    public sealed class SyntheticVariation
    {
        private readonly Vector2[] _itemOffsets;
        private readonly float[] _itemYawDegrees;
        private readonly float[] _itemLyingYawDegrees;
        private readonly bool[] _itemLying;

        private SyntheticVariation(int slotCount)
        {
            _itemOffsets = new Vector2[slotCount];
            _itemYawDegrees = new float[slotCount];
            _itemLyingYawDegrees = new float[slotCount];
            _itemLying = new bool[slotCount];
        }

        /// <summary>박스가 기준 위치에서 벗어난 거리(m). X는 가로, Y는 세로(Z) 방향이다.</summary>
        public Vector2 BoxOffset { get; private set; }

        public float BoxYawDegrees { get; private set; }

        /// <summary>조명 세기 배율(기준 1).</summary>
        public float LightIntensityScale { get; private set; }

        public float LightYawOffsetDegrees { get; private set; }

        public float LightPitchOffsetDegrees { get; private set; }

        /// <summary>조명 색의 따뜻함. 0이 씬의 조명 색, -1이 흰색, +1이 씬보다 두 배 따뜻한 색이다.</summary>
        public float LightWarmth { get; private set; }

        public float HueShift { get; private set; }

        public float SaturationScale { get; private set; }

        public float ValueScale { get; private set; }

        public int SlotCount => _itemOffsets.Length;

        /// <summary>슬롯 번호(1부터)의 구성품이 슬롯 중심에서 벗어난 거리(m). X는 가로, Y는 세로(Z) 방향이다.</summary>
        public Vector2 GetItemOffset(int slotNumber) => _itemOffsets[slotNumber - 1];

        /// <summary>슬롯 번호(1부터)의 구성품이 서 있을 때(판과 상자는 항상) 제자리에서 돈 각도(도). ItemYawDegrees 범위이다.</summary>
        public float GetItemYawDegrees(int slotNumber) => _itemYawDegrees[slotNumber - 1];

        /// <summary>슬롯 번호(1부터)의 구성품이 옆으로 누워 있을 때 제자리에서 돈 각도(도). ItemLyingYawDegrees 범위이다. 눕는 구성품에만 쓴다.</summary>
        public float GetItemLyingYawDegrees(int slotNumber) => _itemLyingYawDegrees[slotNumber - 1];

        /// <summary>슬롯 번호(1부터)의 구성품이 옆으로 누워 있어야 하면 true. 원기둥이 아닌 구성품은 촬영하는 쪽이 무시한다.</summary>
        public bool IsItemLying(int slotNumber) => _itemLying[slotNumber - 1];

        public static SyntheticVariation Create(int seed, RandomizationRanges ranges, int slotCount)
        {
            if (slotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount));
            }

            var random = new System.Random(seed);
            var variation = new SyntheticVariation(slotCount)
            {
                BoxOffset = new Vector2(Symmetric(random, ranges.BoxOffsetMeters), Symmetric(random, ranges.BoxOffsetMeters)),
                BoxYawDegrees = Symmetric(random, ranges.BoxYawDegrees),
                LightIntensityScale = 1f + Symmetric(random, ranges.LightIntensityDeviation),
                LightYawOffsetDegrees = Symmetric(random, ranges.LightYawDegrees),
                LightPitchOffsetDegrees = Symmetric(random, ranges.LightPitchDegrees),
                HueShift = Symmetric(random, ranges.HueShift),
                SaturationScale = 1f + Symmetric(random, ranges.SaturationDeviation),
                ValueScale = 1f + Symmetric(random, ranges.ValueDeviation),
            };

            for (int i = 0; i < slotCount; i++)
            {
                variation._itemOffsets[i] = new Vector2(Symmetric(random, ranges.ItemOffsetMeters), Symmetric(random, ranges.ItemOffsetMeters));
                variation._itemYawDegrees[i] = Symmetric(random, ranges.ItemYawDegrees);
            }

            // 맨 마지막에 뽑아서, 이 값을 추가하기 전의 시드가 만들던 다른 값들이 바뀌지 않게 한다.
            variation.LightWarmth = Symmetric(random, ranges.LightWarmthDeviation);

            // 눕는 자세는 그 뒤에 뽑는다. 이 값들을 더하기 전의 시드가 만들던 다른 값이 바뀌지 않게 하려는 것이다.
            // 확률이 0이어도 난수를 소비해, 확률 하나를 바꿔도 다른 슬롯의 값이 달라지지 않게 한다.
            for (int i = 0; i < slotCount; i++)
            {
                bool lying = random.NextDouble() < Math.Max(0f, ranges.ItemLyingChance);
                float lyingYaw = Symmetric(random, ranges.ItemLyingYawDegrees);
                variation._itemLying[i] = lying;
                // 서 있을 때의 각도는 그대로 둔다. 누운 각도는 따로 두어, 눕지 않는 구성품(판, 상자)이 눕는 확률에 걸려도 ±20도 안에 머문다.
                variation._itemLyingYawDegrees[i] = lyingYaw;
            }

            return variation;
        }

        // 범위가 0이어도 난수를 하나씩 소비한다. 범위 하나를 0으로 바꿔도 나머지 값이 달라지지 않게 하려는 것이다.
        private static float Symmetric(System.Random random, float halfRange)
        {
            double unit = random.NextDouble() * 2.0 - 1.0;
            return (float)(unit * Math.Max(0f, halfRange));
        }
    }
}
