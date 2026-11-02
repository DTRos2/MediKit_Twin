namespace MediKitTwin.Common
{
    /// <summary>
    /// 카메라로 선반 물건의 자세를 알아내는 쪽. 로봇암은 이 계약만 알아서, 시뮬레이션 추정기와 실제 영상 인식기를 같은 방식으로 바꿔 끼운다.
    /// 지금 카메라가 보고 있는 물건을 찾는다. 못 찾으면 Detected가 아닌 결과를 돌려준다.
    /// </summary>
    public interface IItemPoseEstimator
    {
        ItemObservation Observe(ShelfPickItem item);
    }
}
