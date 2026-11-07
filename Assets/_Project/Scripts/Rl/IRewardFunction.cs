namespace MediKitTwin.Rl
{
    /// <summary>보상 계산. 과제(도달, 파지, 들어올리기)마다 구현체만 바꾼다.</summary>
    public interface IRewardFunction
    {
        float Evaluate(in RlContext context);

        /// <summary>성공 조건. 만족하면 에피소드를 성공으로 끝낸다.</summary>
        bool IsSuccess(in RlContext context);
    }
}
