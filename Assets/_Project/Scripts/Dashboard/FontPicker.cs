using System;
using System.Collections.Generic;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 글꼴 이름 후보를 앞에서부터 하나씩 시도해서 처음으로 성공한 것을 돌려준다. 시도하는 방법(글꼴 만들기)은 받아서 쓰므로 Unity 없이 시험할 수 있다.
    /// 이름 목록에 맨 앞 글꼴이 이 컴퓨터에 없으면 건너뛰고 다음 후보로 넘어가는 것이 목적이다.
    /// </summary>
    public static class FontPicker
    {
        /// <param name="names">우선순위 순서의 글꼴 이름.</param>
        /// <param name="tryCreate">이름으로 쓸 수 있는 글꼴을 만들어 돌려주고, 쓸 수 없으면 null을 돌려준다.</param>
        /// <returns>처음으로 만들어진 글꼴. 하나도 못 만들면 null.</returns>
        public static T FirstUsable<T>(IEnumerable<string> names, Func<string, T> tryCreate) where T : class
        {
            if (names == null)
            {
                throw new ArgumentNullException(nameof(names));
            }

            if (tryCreate == null)
            {
                throw new ArgumentNullException(nameof(tryCreate));
            }

            foreach (string name in names)
            {
                T created = tryCreate(name);
                if (created != null)
                {
                    return created;
                }
            }

            return null;
        }
    }
}
