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

    /// <summary>true/false 필드에 값을 넣는다. 필드를 찾지 못하면 false이다.</summary>
    public static bool SetBool(Object target, string fieldName, bool value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null || property.propertyType != SerializedPropertyType.Boolean)
        {
            Debug.LogError($"[SerializedFieldAssigner] {target.GetType().Name}에 true/false 필드 '{fieldName}'이 없다.", target);
            return false;
        }

        property.boolValue = value;
        serialized.ApplyModifiedProperties();
        return true;
    }

    /// <summary>글(string) 필드에 값을 넣는다. 필드를 찾지 못하면 false이다.</summary>
    public static bool SetString(Object target, string fieldName, string value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null || property.propertyType != SerializedPropertyType.String)
        {
            Debug.LogError($"[SerializedFieldAssigner] {target.GetType().Name}에 글(string) 필드 '{fieldName}'이 없다.", target);
            return false;
        }

        property.stringValue = value;
        serialized.ApplyModifiedProperties();
        return true;
    }

    /// <summary>숫자(float) 필드에 값을 넣는다. 필드를 찾지 못하면 false이다.</summary>
    public static bool SetFloat(Object target, string fieldName, float value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null || property.propertyType != SerializedPropertyType.Float)
        {
            Debug.LogError($"[SerializedFieldAssigner] {target.GetType().Name}에 숫자(float) 필드 '{fieldName}'이 없다.", target);
            return false;
        }

        property.floatValue = value;
        serialized.ApplyModifiedProperties();
        return true;
    }

    /// <summary>enum 필드에 값을 넣는다. 필드를 찾지 못하면 false이다.</summary>
    public static bool SetEnum<TEnum>(Object target, string fieldName, TEnum value) where TEnum : System.Enum
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null || property.propertyType != SerializedPropertyType.Enum)
        {
            Debug.LogError($"[SerializedFieldAssigner] {target.GetType().Name}에 enum 필드 '{fieldName}'이 없다.", target);
            return false;
        }

        // enumValueIndex는 정의된 순서의 번호이지 enum의 숫자 값이 아니므로, 값이 건너뛰는 enum도 맞게 찾아 넣는다.
        property.enumValueIndex = System.Array.IndexOf(System.Enum.GetValues(typeof(TEnum)), value);
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
