namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 키팅 진행을 지켜보는 쪽. KittingProcess는 진행 상황을 이 인터페이스로만 알리므로,
    /// 화면 표시, 로그, 시험용 기록기를 키팅 순서를 고치지 않고 붙일 수 있다.
    /// </summary>
    public interface IKittingObserver
    {
        /// <summary>키트 1개의 키팅을 시작한다.</summary>
        void PlanStarted(KittingPlan plan);

        /// <summary>계획의 itemIndex번째 항목(0부터)의 단계가 바뀌었다.</summary>
        void ItemStateChanged(int itemIndex, KittingItemState state);

        /// <summary>키팅이 끝났다(완료, 재고 부족, 오류).</summary>
        void Finished(KittingPlan plan, KittingOutcome outcome);
    }
}
