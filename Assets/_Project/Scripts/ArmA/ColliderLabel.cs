using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 충돌체를 로그에서 알아볼 이름으로 바꾼다.
    /// "Floor", "Cube"처럼 어디에나 있는 이름은 어느 물체의 것인지 알 수 없어 겹친 곳을 찾기 어렵다(컨베이어는 모두 Model/Cube를 갖는다).
    /// 조상 이름을 두 단계까지 앞에 붙여 구분한다. 더 길면 로그가 읽기 어렵다.
    /// </summary>
    public static class ColliderLabel
    {
        public static string Of(Collider collider)
        {
            Transform parent = collider.transform.parent;
            Transform grandparent = parent != null ? parent.parent : null;
            if (parent == null)
            {
                return collider.name;
            }

            return grandparent != null ? $"{grandparent.name}/{parent.name}/{collider.name}" : $"{parent.name}/{collider.name}";
        }
    }
}
