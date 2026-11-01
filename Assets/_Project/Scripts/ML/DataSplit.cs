namespace MediKitTwin.ML
{
    /// <summary>데이터셋 분할. 테스트 세트는 학습에 쓰지 않고, 정상과 불량의 수를 같게 만든다. (FR-08)</summary>
    public enum DataSplit
    {
        Train,
        Validation,
        Test
    }
}
