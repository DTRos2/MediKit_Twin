namespace MediKitTwin.Demo
{
    /// <summary>
    /// 베이스를 손으로 움직이는 입력 한 프레임분. 키 이름과 상관없이 방향만 담으므로, 키보드든 다른 입력이든 같은 계산으로 처리한다.
    /// 앞뒤와 좌우는 베이스가 보는 방향 기준이다.
    /// </summary>
    public readonly struct BaseMoveInput
    {
        /// <param name="forward">앞(+1)과 뒤(-1).</param>
        /// <param name="right">오른쪽(+1)과 왼쪽(-1).</param>
        /// <param name="turn">오른쪽으로 돌기(+1)와 왼쪽으로 돌기(-1).</param>
        /// <param name="isFast">빠르게 움직인다.</param>
        public BaseMoveInput(float forward, float right, float turn, bool isFast)
        {
            Forward = forward;
            Right = right;
            Turn = turn;
            IsFast = isFast;
        }

        public float Forward { get; }

        public float Right { get; }

        public float Turn { get; }

        public bool IsFast { get; }

        /// <summary>움직이거나 돌라는 입력이 없다. 빠르게 키만 눌린 것은 입력으로 치지 않는다.</summary>
        public bool IsIdle => Forward == 0f && Right == 0f && Turn == 0f;
    }
}
