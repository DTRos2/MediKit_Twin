using UnityEditor;
using UnityEngine;

// 프로젝트 설정(TagManager)에 레이어 이름을 등록한다. 이미 있으면 그대로 두므로 몇 번을 불러도 같은 결과가 된다.
// 사용자 레이어는 8번부터 31번까지이고, 빈 칸 중 가장 앞에 넣는다.
public static class LayerRegistrar
{
    private const string LayersProperty = "layers";
    private const int FirstUserLayer = 8;

    /// <summary>레이어 번호를 돌려준다. 이름이 비었거나 빈 칸이 없어 만들 수 없으면 -1이다.</summary>
    public static int EnsureLayer(string layerName)
    {
        if (string.IsNullOrEmpty(layerName))
        {
            return -1;
        }

        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0)
        {
            return existing;
        }

        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty(LayersProperty);

        for (int index = FirstUserLayer; index < layers.arraySize; index++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(index);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return index;
            }
        }

        Debug.LogError($"[LayerRegistrar] 레이어 '{layerName}'을 만들 빈 칸이 없다. Project Settings > Tags and Layers에서 칸을 비운다.");
        return -1;
    }
}
