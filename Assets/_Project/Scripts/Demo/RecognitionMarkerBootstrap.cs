using MediKitTwin.ArmA;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 씬이 시작될 때 로봇암 이송(ArmItemTransfer)이 있는데 인식을 보여 주는 화면이 없으면 만든다.
    ///  - 인식 표시(RecognitionMarkerOverlay): 메인 화면의 물건 자리에 초록 네모.
    ///  - 카메라 화면(ToolCameraDemoPanel): 물건을 찾아볼 때 카메라가 본 화면.
    ///  - 로그 패널(KittingLogDemoPanel): 키팅과 로봇의 로그를 모아 복사할 수 있게 하는 화면.
    /// 화면을 씬에 따로 넣지 않아도 인식한 물건과 로그를 볼 수 있게 하려는 것이다. 이미 있는 것은 건드리지 않는다.
    /// </summary>
    public static class RecognitionMarkerBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRecognitionViews()
        {
            if (Object.FindFirstObjectByType<ArmItemTransfer>() == null)
            {
                return;
            }

            EnsureSingle<RecognitionMarkerOverlay>("RecognitionMarker");
            EnsureSingle<ToolCameraDemoPanel>("ToolCameraPanel");
            EnsureSingle<KittingLogDemoPanel>("KittingLogPanel");
        }

        // 같은 종류가 씬에 이미 있으면 아무것도 하지 않고, 없으면 이름 붙인 새 오브젝트에 하나 단다.
        private static void EnsureSingle<T>(string objectName) where T : Component
        {
            if (Object.FindFirstObjectByType<T>() != null)
            {
                return;
            }

            new GameObject(objectName).AddComponent<T>();
            Debug.Log($"[{nameof(RecognitionMarkerBootstrap)}] {typeof(T).Name}을(를) 만들었다 ({objectName}).");
        }
    }
}
