using System;
using UnityEngine;

namespace MediKitTwin.Inspection
{
    /// <summary>
    /// 슬롯 크롭을 모델 입력(3 x 72 x 128, RGB, 0~1, NCHW)으로 바꾼다.
    /// 학습은 PIL의 BILINEAR 축소(MLTraining/data.py)를 쓰므로, 같은 결과가 나오도록 축소 비율만큼 넓힌
    /// 삼각 필터를 가로, 세로로 차례로 적용한다. 단순 이중선형 샘플링과는 값이 조금 다르다.
    /// </summary>
    public static class SlotTensorizer
    {
        public const int Width = 128;
        public const int Height = 72;
        public const int Channels = 3;
        public const int FloatsPerImage = Channels * Height * Width;

        /// <summary>이미지 하나를 destination[offset ..]에 채운다. destination은 offset + FloatsPerImage 이상이어야 한다.</summary>
        public static void Write(Texture2D image, float[] destination, int offset)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            Color32[] pixels = image.GetPixels32(); // 왼쪽 아래 원점
            int srcW = image.width;
            int srcH = image.height;

            // 가로 축소: 원본 행(위에서부터) x Width
            var horizontal = new float[srcH * Width * Channels];
            FilterWeights xs = FilterWeights.Build(srcW, Width);
            for (int row = 0; row < srcH; row++)
            {
                int srcRow = srcH - 1 - row; // 위쪽이 0
                for (int x = 0; x < Width; x++)
                {
                    float r = 0f, g = 0f, b = 0f;
                    for (int k = 0; k < xs.Counts[x]; k++)
                    {
                        float w = xs.Weights[x][k];
                        Color32 c = pixels[srcRow * srcW + xs.Starts[x] + k];
                        r += c.r * w;
                        g += c.g * w;
                        b += c.b * w;
                    }

                    int o = (row * Width + x) * Channels;
                    horizontal[o] = r;
                    horizontal[o + 1] = g;
                    horizontal[o + 2] = b;
                }
            }

            // 세로 축소: Height x Width, 채널 평면(NCHW)으로 기록
            FilterWeights ys = FilterWeights.Build(srcH, Height);
            const float Scale = 1f / 255f;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float r = 0f, g = 0f, b = 0f;
                    for (int k = 0; k < ys.Counts[y]; k++)
                    {
                        float w = ys.Weights[y][k];
                        int o = ((ys.Starts[y] + k) * Width + x) * Channels;
                        r += horizontal[o] * w;
                        g += horizontal[o + 1] * w;
                        b += horizontal[o + 2] * w;
                    }

                    int pixel = y * Width + x;
                    destination[offset + pixel] = Mathf.Clamp01(r * Scale);
                    destination[offset + Height * Width + pixel] = Mathf.Clamp01(g * Scale);
                    destination[offset + 2 * Height * Width + pixel] = Mathf.Clamp01(b * Scale);
                }
            }
        }

        private sealed class FilterWeights
        {
            public int[] Starts;
            public int[] Counts;
            public float[][] Weights;

            // PIL의 ImagingResample 삼각(bilinear) 필터: 축소일 때 지지 범위와 폭을 비율만큼 넓히고 가중치 합을 1로 맞춘다.
            public static FilterWeights Build(int inputSize, int outputSize)
            {
                float scale = (float)inputSize / outputSize;
                float filterScale = Mathf.Max(scale, 1f);
                float support = 1f * filterScale;

                var result = new FilterWeights
                {
                    Starts = new int[outputSize],
                    Counts = new int[outputSize],
                    Weights = new float[outputSize][]
                };

                for (int i = 0; i < outputSize; i++)
                {
                    float center = (i + 0.5f) * scale;
                    int start = Mathf.Max((int)(center - support + 0.5f), 0);
                    int end = Mathf.Min((int)(center + support + 0.5f), inputSize);
                    int count = end - start;

                    var weights = new float[count];
                    float sum = 0f;
                    for (int k = 0; k < count; k++)
                    {
                        float d = (start + k - center + 0.5f) / filterScale;
                        float w = Mathf.Max(0f, 1f - Mathf.Abs(d));
                        weights[k] = w;
                        sum += w;
                    }

                    if (sum > 0f)
                    {
                        for (int k = 0; k < count; k++)
                        {
                            weights[k] /= sum;
                        }
                    }

                    result.Starts[i] = start;
                    result.Counts[i] = count;
                    result.Weights[i] = weights;
                }

                return result;
            }
        }
    }
}
