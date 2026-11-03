using UnityEngine;

namespace MediKitTwin.Inspection
{
    /// <summary>
    /// 검사 이미지에서 박스 밖에 보이는 배경색. 검사 카메라(InspectionCameraRig)와 합성 데이터 생성기(SyntheticBoxRenderer)가 같은 값을 써야
    /// 학습 이미지와 촬영 이미지가 같아진다. 씬에서는 박스 밖에 어두운 컨베이어가 보이므로 그 색에 맞춘다.
    /// 값은 2026-11-03 씬 촬영 샘플 12장의 모서리 평균(밝기 45~58)이다. 박스가 놓이는 곳이 바뀌면 다시 잰다.
    /// </summary>
    public static class InspectionBackground
    {
        public static readonly Color Color = new Color(0.195f, 0.2f, 0.205f);
    }
}
