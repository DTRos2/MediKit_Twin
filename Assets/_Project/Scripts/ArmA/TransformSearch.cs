using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>URDF 링크 이름으로 하위 오브젝트를 찾는다. 링크 GameObject 이름이 곧 URDF 링크 이름이다.</summary>
    internal static class TransformSearch
    {
        public static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform found = FindDeep(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
