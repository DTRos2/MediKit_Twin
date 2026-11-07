using System;
using UnityEngine;

namespace MediKitTwin.Label
{
    /// <summary>
    /// 박스 위에 덮인 뚜껑과 그 윗면 자리를 찾는다. (FR-11)
    /// 뚜껑은 이름이 머리말(Lid_)로 시작하는 오브젝트이고, 윗면은 그 아래 모든 렌더러를 감싼 범위의 맨 위 가운데로 본다.
    /// </summary>
    public static class LidSurface
    {
        /// <param name="root">뚜껑을 찾을 범위의 뿌리(박스를 싣는 받침).</param>
        /// <param name="lidPrefix">뚜껑 이름의 머리말.</param>
        /// <param name="lid">찾은 뚜껑.</param>
        /// <param name="topCenter">뚜껑 윗면 가운데(월드).</param>
        public static bool TryFind(Transform root, string lidPrefix, out Transform lid, out Vector3 topCenter)
        {
            lid = null;
            topCenter = default;
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>())
            {
                if (candidate.name.StartsWith(lidPrefix, StringComparison.Ordinal))
                {
                    lid = candidate;
                    break;
                }
            }

            if (lid == null)
            {
                return false;
            }

            bool hasBounds = false;
            var bounds = new Bounds(lid.position, Vector3.zero);
            foreach (Renderer renderer in lid.GetComponentsInChildren<Renderer>())
            {
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            topCenter = hasBounds ? new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) : lid.position;
            return true;
        }
    }
}
