using UnityEditor;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// PF_Pusher에 푸셔(Pusher 컴포넌트)를 붙이고 헤드 높이를 맞춘다. (FR-09)
    /// 프리팹을 다시 만들지 않고 기존 오브젝트에 더한다. 다시 만들면 씬의 오버라이드가 풀릴 수 있다. 여러 번 실행해도 결과가 같다.
    /// 헤드 높이는 CellSceneBuilder.BuildPusher와 같은 값(컨베이어 CenterPoint 높이 0.91m + 0.05m)이다.
    /// </summary>
    public static class PusherSetup
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/PF_Pusher.prefab";
        private const string HeadName = "PushHead";

        // 헤드 중심의 로컬 높이(m). 바닥 0.86m는 컨베이어 본체 충돌체 윗면(0.85m)보다 1cm 위이다.
        private const float HeadCenterY = 0.96f;

        [MenuItem("Tools/MediKit/Setup Pusher Prefab")]
        public static void Setup()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogError($"[PusherSetup] 프리팹이 없습니다: {PrefabPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform head = root.transform.Find(HeadName);
                if (head == null)
                {
                    Debug.LogError($"[PusherSetup] PF_Pusher에 {HeadName}이 있어야 합니다.");
                    return;
                }

                bool changed = false;
                if (root.GetComponent<MediKitTwin.Pusher.Pusher>() == null)
                {
                    root.AddComponent<MediKitTwin.Pusher.Pusher>();
                    changed = true;
                }

                Vector3 position = head.localPosition;
                if (!Mathf.Approximately(position.y, HeadCenterY))
                {
                    position.y = HeadCenterY;
                    head.localPosition = position;
                    changed = true;
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                    AssetDatabase.SaveAssets();
                }

                Debug.Log($"[PusherSetup] PF_Pusher: Pusher 컴포넌트와 헤드 높이 y={HeadCenterY:F2}m {(changed ? "적용" : "이미 맞음")}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
