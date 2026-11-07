using UnityEngine;

namespace MediKitTwin.ML
{
    /// <summary>합성 이미지에서 직육면체 구성품이 어떤 모양인지. 모양에 따라 놓이는 자세가 다르다.</summary>
    public enum ItemFlatness
    {
        /// <summary>두꺼운 판이나 상자(거즈, 장갑). 서 있는 모양 그대로 놓는다.</summary>
        Block = 0,

        /// <summary>얇은 판(시험 카세트, 샘플 백, 알코올 패드). 칸막이에 기대거나 긴 모서리로 설 수 있다.</summary>
        Plate = 1,

        /// <summary>가는 막대(면봉, 바늘). 한쪽 끝이 들려 기울 수 있다.</summary>
        Rod = 2
    }

    /// <summary>얇은 판이나 가는 막대의 자세. 박스 기준 회전과, 그 회전으로 바닥에 닿게 놓았을 때 중심의 높이이다.</summary>
    public readonly struct ItemPose
    {
        public ItemPose(Quaternion localRotation, float centerHeight)
        {
            LocalRotation = localRotation;
            CenterHeight = centerHeight;
        }

        public Quaternion LocalRotation { get; }

        /// <summary>바닥면에서 구성품 중심까지의 높이(m). 회전한 모양의 가장 낮은 점이 바닥에 닿는다.</summary>
        public float CenterHeight { get; }
    }

    /// <summary>
    /// 직육면체 구성품의 자세를 계산한다. 장면을 읽지 않는 계산이라 단독으로 시험할 수 있다.
    /// 키팅에서 얇은 판과 가는 막대는 바닥에 평평하게만 놓이지 않고, 칸막이에 기대 비스듬히 놓이거나 판이 모서리로 서기도 한다.
    /// 이런 자세가 학습 데이터에 없으면 그 구성품을 다른 구성품이나 빈 슬롯으로 읽는다.
    /// </summary>
    public static class ItemPosePlanner
    {
        /// <summary>납작한 정도의 기준: 가장 얇은 변이 넓은 면의 짧은 변의 이 비율 이하면 판이다. 거즈(0.30)와 장갑은 이 기준을 넘어 Block이다.</summary>
        public const float PlateMaxThickness = 0.25f;

        /// <summary>가는 정도의 기준: 단면의 큰 변이 길이의 이 비율 이하면 막대이다.</summary>
        public const float RodMaxSection = 0.25f;

        // 모서리로 서 있을 때는 불안정해서 크게 기울지 않는다. 기울기를 이 비율로 줄인다.
        private const float EdgeStandTiltScale = 0.3f;

        /// <summary>크기(X 옆, Y 위, Z 앞)로 구성품이 판인지, 막대인지, 아닌지 가른다. 크기를 같은 비율로 줄여도 결과는 같다.</summary>
        public static ItemFlatness Classify(Vector3 size)
        {
            float shortSide = Mathf.Min(size.x, size.z);
            if (size.y <= PlateMaxThickness * shortSide)
            {
                return ItemFlatness.Plate;
            }

            if (Mathf.Max(size.x, size.y) <= RodMaxSection * size.z)
            {
                return ItemFlatness.Rod;
            }

            return ItemFlatness.Block;
        }

        /// <summary>
        /// 자세를 계산한다. 회전은 (제자리 회전) × (기울기) × (모서리로 세우기) 순서이다.
        /// 막대는 앞뒤 기울기(pitch)만 쓰고, 판은 앞뒤와 좌우 기울기와 모서리로 서기를 쓴다. Block은 제자리 회전만 한다.
        /// </summary>
        public static ItemPose Plan(Vector3 size, ItemFlatness kind, float yawDegrees, float pitchDegrees, float rollDegrees, bool edgeStand, float edgeSign)
        {
            Quaternion rest = Quaternion.identity;
            float tiltScale = 1f;
            if (kind == ItemFlatness.Plate && edgeStand)
            {
                // 긴 모서리를 바닥에 대고 세운다. 짧은 모서리로 세우면 불안정해서 현실적이지 않다.
                float sign = edgeSign < 0f ? -1f : 1f;
                rest = size.x >= size.z
                    ? Quaternion.Euler(90f * sign, 0f, 0f)
                    : Quaternion.Euler(0f, 0f, 90f * sign);
                tiltScale = EdgeStandTiltScale;
            }

            Quaternion tilt = Quaternion.identity;
            if (kind == ItemFlatness.Plate)
            {
                tilt = Quaternion.Euler(pitchDegrees * tiltScale, 0f, rollDegrees * tiltScale);
            }
            else if (kind == ItemFlatness.Rod)
            {
                tilt = Quaternion.Euler(pitchDegrees, 0f, 0f);
            }

            Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f) * tilt * rest;
            return new ItemPose(rotation, HalfHeight(rotation, size));
        }

        // 회전한 직육면체가 세로(Y) 방향으로 차지하는 높이의 절반. 이만큼 띄우면 가장 낮은 점이 바닥에 닿는다.
        private static float HalfHeight(Quaternion rotation, Vector3 size)
        {
            Vector3 x = rotation * Vector3.right;
            Vector3 y = rotation * Vector3.up;
            Vector3 z = rotation * Vector3.forward;
            return 0.5f * (Mathf.Abs(x.y) * size.x + Mathf.Abs(y.y) * size.y + Mathf.Abs(z.y) * size.z);
        }
    }
}
