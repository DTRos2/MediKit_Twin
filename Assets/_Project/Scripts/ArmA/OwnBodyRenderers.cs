using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 로봇 자기 몸에 속한 렌더러를 모은다. 쥔 물건(Graspable)은 로봇 아래에 붙어 있어도 뺀다: 카메라가 집은 물건은 봐야 한다.
    /// 장면을 읽는 일만 한다.
    /// </summary>
    public static class OwnBodyRenderers
    {
        public static List<Renderer> Collect(Transform robotRoot)
        {
            var found = new List<Renderer>();
            if (robotRoot == null)
            {
                return found;
            }

            foreach (Renderer renderer in robotRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.GetComponentInParent<Graspable>() == null)
                {
                    found.Add(renderer);
                }
            }

            return found;
        }
    }
}
