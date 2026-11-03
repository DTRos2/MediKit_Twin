using System.Collections.Generic;
using MediKitTwin.Inventory;
using UnityEngine;

namespace MediKitTwin.EditorTools
{
    /// <summary>
    /// 키트 박스(KitBox)에 슬롯 사이 칸막이(Divider_N)를 붙인다. 지그 프리팹을 고치는 도구와
    /// 씬 빌더가 같은 방식으로 만들도록 모았다. 치수는 박스의 바닥과 벽에서 읽고, 자리와 두께는
    /// 설정(KitBoxPartitionConfigSO)이 정한다. 몇 번을 불러도 칸막이는 항상 새로 만든 한 벌만 남는다.
    /// </summary>
    public static class KitBoxPartitionBuilder
    {
        public const string DividerPrefix = "Divider_";

        private const string FloorName = "Floor";
        private const string WallNegXName = "Wall_NegX";
        private const string WallPosXName = "Wall_PosX";

        /// <summary>kitBox 아래에 칸막이를 새로 만든다. 만든 칸막이의 개수를 돌려주고, 박스 구조를 읽지 못하면 -1이다.</summary>
        public static int Rebuild(Transform kitBox, KitBoxPartitionConfigSO config)
        {
            Transform floor = kitBox.Find(FloorName);
            Transform wallNegX = kitBox.Find(WallNegXName);
            Transform wallPosX = kitBox.Find(WallPosXName);
            if (floor == null || wallNegX == null || wallPosX == null)
            {
                Debug.LogError($"[KitBoxPartitionBuilder] {kitBox.name} 아래에 {FloorName}, {WallNegXName}, {WallPosXName}이(가) 모두 있어야 칸막이를 붙일 수 있습니다.");
                return -1;
            }

            Remove(kitBox);

            KitBoxPartitionLayout layout = config.CreateLayout();

            // 박스 안쪽: 바닥 윗면에서 양쪽 벽의 안쪽 면 사이. 벽은 큐브라 로컬 스케일이 곧 치수이다.
            float floorTop = floor.localPosition.y + floor.localScale.y * 0.5f;
            float rim = Mathf.Max(
                wallNegX.localPosition.y + wallNegX.localScale.y * 0.5f,
                wallPosX.localPosition.y + wallPosX.localScale.y * 0.5f);
            float innerMinX = wallNegX.localPosition.x + wallNegX.localScale.x * 0.5f;
            float innerMaxX = wallPosX.localPosition.x - wallPosX.localScale.x * 0.5f;
            float innerWidth = innerMaxX - innerMinX;
            float centerX = (innerMinX + innerMaxX) * 0.5f;

            // 칸막이가 벽보다 높으면 뚜껑 덮기가 박스 윗면을 칸막이 높이로 재서 뚜껑을 너무 높게 얹는다.
            float height = Mathf.Min(config.HeightM, rim - floorTop);
            if (height < config.HeightM)
            {
                Debug.LogWarning($"[KitBoxPartitionBuilder] 칸막이 높이 {config.HeightM:0.000}m가 벽 높이({rim - floorTop:0.000}m)보다 커서 벽 높이에 맞춥니다.");
            }

            Material material = floor.GetComponent<Renderer>().sharedMaterial;
            IReadOnlyList<float> centers = layout.DividerCenters;
            for (int i = 0; i < centers.Count; i++)
            {
                GameObject divider = GameObject.CreatePrimitive(PrimitiveType.Cube);
                divider.name = DividerPrefix + (i + 1);
                divider.layer = floor.gameObject.layer;
                divider.transform.SetParent(kitBox, false);
                divider.transform.localPosition = new Vector3(centerX, floorTop + height * 0.5f, centers[i]);
                divider.transform.localScale = new Vector3(innerWidth, height, layout.Thickness);
                divider.GetComponent<Renderer>().sharedMaterial = material;
            }

            return centers.Count;
        }

        /// <summary>kitBox 아래의 칸막이를 모두 지운다. 지운 개수를 돌려준다.</summary>
        public static int Remove(Transform kitBox)
        {
            var stale = new List<GameObject>();
            foreach (Transform child in kitBox)
            {
                if (child.name.StartsWith(DividerPrefix))
                {
                    stale.Add(child.gameObject);
                }
            }

            foreach (GameObject divider in stale)
            {
                Object.DestroyImmediate(divider);
            }

            return stale.Count;
        }
    }
}
