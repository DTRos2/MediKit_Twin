#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 충돌체가 차지하는 공간을 대표하는 월드 점들을 돌려준다. 메쉬 충돌체는 정점, 상자는 모서리 여덟 개,
    /// 그 밖의 모양은 월드 경계 상자의 모서리다. 퍼진 정도를 재려는 것이라 점이 충돌체를 조금 넓게 덮어도 괜찮다.
    /// </summary>
    public static class ColliderPoints
    {
        // 정점이 이보다 많은 메쉬는 경계 상자로 갈음한다(한 번 재는 데 오래 걸리지 않게).
        private const int MaxMeshVertices = 20000;

        public static IEnumerable<Vector3> Of(Collider collider)
        {
            if (collider is MeshCollider meshCollider && meshCollider.sharedMesh != null && meshCollider.sharedMesh.vertexCount <= MaxMeshVertices)
            {
                foreach (Vector3 vertex in meshCollider.sharedMesh.vertices)
                {
                    yield return collider.transform.TransformPoint(vertex);
                }

                yield break;
            }

            if (collider is BoxCollider box)
            {
                foreach (Vector3 corner in Corners(box.center, box.size * 0.5f))
                {
                    yield return collider.transform.TransformPoint(corner);
                }

                yield break;
            }

            Bounds bounds = collider.bounds;
            foreach (Vector3 corner in Corners(bounds.center, bounds.extents))
            {
                yield return corner;
            }
        }

        private static IEnumerable<Vector3> Corners(Vector3 center, Vector3 half)
        {
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        yield return center + new Vector3(half.x * x, half.y * y, half.z * z);
                    }
                }
            }
        }
    }
}
#endif
