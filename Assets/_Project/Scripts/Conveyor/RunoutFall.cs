namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 컨베이어 끝을 넘은 박스가 받침대로 내려앉는 높이를 계산한다. 박스는 컨베이어 에셋의 Parcel이라 물리로 떨어지지 않고
    /// 받침대 윗면에 정확히 앉아야 하므로, 중력 가속으로 내려오다 앉을 높이에서 멈추게 한다. 시간은 호출하는 쪽이 시뮬레이션 시간(Time.deltaTime)으로 준다.
    /// </summary>
    public static class RunoutFall
    {
        /// <summary>
        /// 한 프레임 내려온 뒤의 높이. speed는 아래로 향할수록 음수이고, 앉을 높이에 닿으면 0으로 되돌린다.
        /// 앉을 높이를 넘어 내려가지 않는다(프레임이 길어도 받침대를 뚫지 않는다).
        /// </summary>
        /// <param name="height">지금 높이(m).</param>
        /// <param name="speed">지금 세로 속도(m/s). 호출마다 갱신된다.</param>
        /// <param name="landingHeight">앉을 높이(m).</param>
        /// <param name="gravity">중력 가속도(m/s², 아래 방향이면 음수).</param>
        /// <param name="deltaTime">지난 시간(초).</param>
        public static float Step(float height, ref float speed, float landingHeight, float gravity, float deltaTime)
        {
            speed += gravity * deltaTime;
            float next = height + speed * deltaTime;
            if (next <= landingHeight)
            {
                speed = 0f;
                return landingHeight;
            }

            return next;
        }
    }
}
