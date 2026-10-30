using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 에디터 도구가 컴포넌트의 private [SerializeField]에 값을 넣는 일을 한 곳에 모은다.
// 필드 이름이 바뀌어 연결이 조용히 빠지는 일이 없도록, 필드를 못 찾으면 바로 알린다.
public static class SerializedFieldAssigner
{
    /// <summary>오브젝트 참조 필드에 값을 넣는다. 필드를 찾지 못하면 false이다.</summary>
    public static bool SetReference(Object target, string fieldName, Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogError($"[SerializedFieldAssigner] {target.GetType().Name}에 '{fieldName}' 필드가 없다.", target);
            return false;
        }

        property.objectReferenceValue = value;
        serialized.ApplyModifiedProperties();
        return true;
    }

    /// <summary>오브젝트 참조 목록 필드를 주어진 값들로 바꾼다. 필드를 찾지 못하면 false이다.</summary>
    public static bool SetReferenceList(Object target, string fieldName, IReadOnlyList<Object> values)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null || !property.isArray)
        {
            Debug.LogError($"[SerializedFieldAssigner] {target.GetType().Name}에 목록 필드 '{fieldName}'이 없다.", target);
            return false;
        }

        property.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        serialized.ApplyModifiedProperties();
        return true;
    }
}
