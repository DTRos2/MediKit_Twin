using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 인터페이스를 구현한 컴포넌트를 씬에서 찾는다. 패널이 구체 클래스 이름이 아니라 필요한 접점(인터페이스)만으로 협력자를
    /// 찾게 해서, 같은 접점을 구현한 다른 컴포넌트로 바꿔도 패널을 고치지 않는다.
    /// </summary>
    public static class DemoComponentFinder
    {
        /// <summary>처음 만나는 구현체를 돌려준다. 없으면 null이다. 켜진 오브젝트만 본다.</summary>
        public static T FindFirst<T>() where T : class
        {
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude))
            {
                if (behaviour is T found)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>구현체를 모두 모아 돌려준다. 로봇이 여러 대일 때 고르는 데 쓴다. 켜진 오브젝트만 본다.</summary>
        public static List<MonoBehaviour> FindAll<T>() where T : class
        {
            return FindAllMatching(behaviour => behaviour is T);
        }

        /// <summary>두 인터페이스를 모두 구현한 컴포넌트만 모아 돌려준다. 같은 컴포넌트가 읽기와 쓰기를 함께 맡아야 할 때 쓴다. 켜진 오브젝트만 본다.</summary>
        public static List<MonoBehaviour> FindAll<TFirst, TSecond>()
            where TFirst : class
            where TSecond : class
        {
            return FindAllMatching(behaviour => behaviour is TFirst && behaviour is TSecond);
        }

        private static List<MonoBehaviour> FindAllMatching(System.Predicate<MonoBehaviour> matches)
        {
            var found = new List<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude))
            {
                if (matches(behaviour))
                {
                    found.Add(behaviour);
                }
            }

            return found;
        }
    }
}
