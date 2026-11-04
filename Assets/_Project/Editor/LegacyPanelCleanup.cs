using MediKitTwin.ArmA;
using MediKitTwin.Conveyor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 예전 IMGUI 패널(코드가 매 프레임 그려 내던 DemoPanel 파생 클래스)을 지운 뒤 씬에 "스크립트 없음"으로 남은 컴포넌트를 뗀다.
    /// 그 패널들이 붙어 있던 오브젝트(키팅 스테이션, 컨베이어 이송 오브젝트, 로봇암)만 보므로, 다른 오브젝트의 빈 컴포넌트는 건드리지 않는다.
    /// 지울 것이 없으면 아무 일도 하지 않으므로 여러 번 불러도 된다.
    /// </summary>
    public static class LegacyPanelCleanup
    {
        public static int RemoveMissingScripts()
        {
            int removed = 0;
            foreach (KittingController station in Object.FindObjectsByType<KittingController>(FindObjectsInactive.Include))
            {
                removed += RemoveFrom(station.gameObject);
            }

            foreach (ConveyorSpeedSetter setter in Object.FindObjectsByType<ConveyorSpeedSetter>(FindObjectsInactive.Include))
            {
                removed += RemoveFrom(setter.gameObject);
            }

            // 로봇암 오브젝트에도 예전 관절 패널(ArmDemoPanel)이 클래스만 사라진 채 붙어 있었다.
            foreach (ArmController arm in Object.FindObjectsByType<ArmController>(FindObjectsInactive.Include))
            {
                removed += RemoveFrom(arm.gameObject);
            }

            return removed;
        }

        private static int RemoveFrom(GameObject host)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(host);
            if (removed > 0)
            {
                Debug.Log($"[LegacyPanelCleanup] {host.name}에서 예전 IMGUI 패널이 남긴 '스크립트 없음' 컴포넌트 {removed}개를 뗐다.");
            }

            return removed;
        }
    }
}
