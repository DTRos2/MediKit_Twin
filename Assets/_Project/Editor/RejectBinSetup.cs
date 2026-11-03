using MediKitTwin.Pusher;
using UnityEditor;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// PF_RejectBin에 안쪽 영역(LoadZone: 트리거 콜라이더 + RejectBin)을 붙인다. (FR-09)
    /// 프리팹을 다시 만들지 않고 자식 하나만 더하거나 크기를 맞춘다. 다시 만들면 씬의 오버라이드가 풀릴 수 있다.
    /// 영역의 크기는 프리팹의 바닥과 벽에서 읽는다: 바닥 윗면에서 벽 윗면보다 조금 위까지, 벽 안쪽 면 사이. 여러 번 실행해도 결과가 같다.
    /// </summary>
    public static class RejectBinSetup
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/PF_RejectBin.prefab";
        private const string ZoneName = "LoadZone";

        // 벽 윗면보다 위로 영역을 늘리는 높이(m). 밀려 떨어지는 박스가 벽을 넘는 순간부터 세기 위해서이다.
        private const float AboveRim = 0.15f;

        [MenuItem("Tools/MediKit/Add Load Zone To RejectBin Prefab")]
        public static void AddLoadZone()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogError($"[RejectBinSetup] 프리팹이 없습니다: {PrefabPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform bin = root.transform.Find("Bin");
                Transform floor = bin != null ? bin.Find("Floor") : null;
                Transform negX = bin != null ? bin.Find("Wall_NegX") : null;
                Transform negZ = bin != null ? bin.Find("Wall_NegZ") : null;
                if (floor == null || negX == null || negZ == null)
                {
                    Debug.LogError("[RejectBinSetup] PF_RejectBin에 Bin/Floor, Wall_NegX, Wall_NegZ가 있어야 합니다.");
                    return;
                }

                // 바닥 윗면, 벽 윗면, 벽 안쪽 면 사이를 Bin 로컬 좌표로 구한다.
                float floorTop = floor.localPosition.y + floor.localScale.y * 0.5f;
                float rimTop = negX.localPosition.y + negX.localScale.y * 0.5f + AboveRim;
                float innerX = floor.localScale.x - 2f * negX.localScale.x;
                float innerZ = floor.localScale.z - 2f * negZ.localScale.z;
                Vector3 localCenter = new Vector3(0f, (floorTop + rimTop) * 0.5f, 0f);
                Vector3 localSize = new Vector3(innerX, rimTop - floorTop, innerZ);

                Transform zone = root.transform.Find(ZoneName);
                bool created = zone == null;
                if (created)
                {
                    zone = new GameObject(ZoneName).transform;
                    zone.SetParent(root.transform, false);
                }

                zone.localPosition = root.transform.InverseTransformPoint(bin.TransformPoint(localCenter));
                zone.localRotation = Quaternion.identity;
                zone.localScale = Vector3.one;

                var box = zone.GetComponent<BoxCollider>();
                if (box == null)
                {
                    box = zone.gameObject.AddComponent<BoxCollider>();
                }

                box.isTrigger = true;
                box.center = Vector3.zero;
                box.size = localSize;

                if (zone.GetComponent<RejectBin>() == null)
                {
                    zone.gameObject.AddComponent<RejectBin>();
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[RejectBinSetup] PF_RejectBin {ZoneName} {(created ? "추가" : "갱신")}: 크기 {localSize.x:F2} x {localSize.y:F2} x {localSize.z:F2}m, 중심 높이 {localCenter.y:F2}m (Bin 기준)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
