namespace MediKitTwin.Common
{
    /// <summary>
    /// 검사 판정기. 규칙 판정(FR-06)과 ML 판정(FR-07)이 같은 입출력을 쓴다.
    /// 판정기는 KitData.GetMissingAnswerForEvaluation()을 호출하지 않는다.
    /// </summary>
    public interface IInspector
    {
        JudgeMethod Method { get; }

        /// <summary>
        /// 키트를 판정한다. 모델 로드 실패나 추론 오류처럼 판정할 수 없으면 false를 돌려주며,
        /// 호출하는 쪽은 규칙 판정기로 대체하고 로그를 남긴다. (FR-07 예외 처리)
        /// 판정 결과를 KitData.Verdict에 넣는 일은 호출하는 쪽이 한다.
        /// </summary>
        bool TryJudge(KitData kit, out InspectionVerdict verdict);
    }
}
