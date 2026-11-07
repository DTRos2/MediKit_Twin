namespace MediKitTwin.Inventory
{
    /// <summary>
    /// 선반 칸 안에서 구성품 1개를 놓을 자리. 칸 중심 기준 가로(X)와 깊이(Z) 오프셋(m)과,
    /// 원래 크기에 곱할 배율이다. 배율이 1보다 작으면 칸이 좁아 구성품을 줄여야 겹치지 않는다는 뜻이다.
    /// </summary>
    public readonly struct ShelfItemPlacement
    {
        public ShelfItemPlacement(float x, float z, float scale)
        {
            X = x;
            Z = z;
            Scale = scale;
        }

        public float X { get; }

        public float Z { get; }

        public float Scale { get; }
    }
}
