using MediKitTwin.Conveyor;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 씬에 팩킹 정지점(PackingStop)이 있으면 팩킹 실패 처리(컨베이어와 팔 A 멈춤)와 경고 패널을 만든다. 이미 있으면 건드리지 않는다.
    /// 씬 파일을 고치지 않고 켜기 위한 것이다.
    /// </summary>
    public static class PackingFailureBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePackingFailureHandling()
        {
            if (Object.FindFirstObjectByType<PackingStop>() == null)
            {
                return;
            }

            if (Object.FindFirstObjectByType<PackingFailureHandler>() == null)
            {
                new GameObject("PackingFailureHandler").AddComponent<PackingFailureHandler>();
            }

            if (Object.FindFirstObjectByType<PackingFailureAlertPanel>() == null)
            {
                new GameObject("PackingFailureAlertPanel").AddComponent<PackingFailureAlertPanel>();
            }
        }
    }
}
