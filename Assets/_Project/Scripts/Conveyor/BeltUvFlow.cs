using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 벨트 메시의 텍스처 좌표(UV)가 물건이 가는 방향(흐름)을 따라 월드 길이 1m당 얼마나 바뀌는지를 잰다.
    /// 벨트 무늬를 흐름 방향으로 밀려면 UV 오프셋을 이 값에 비례해 움직여야 하는데, 메시마다 UV가 흐르는 축, 부호, 반복 길이가 달라서
    /// 축이나 길이를 코드에 박아 두지 않고 정점에서 직접 구한다. 윗면(위를 보는 면)의 정점만 쓴다. 위로 보이는 면이 아니면 무늬가 보이지 않기 때문이다.
    /// </summary>
    public static class BeltUvFlow
    {
        /// <param name="flowDirection">흐름 방향(월드). 길이가 0이면 잴 수 없다.</param>
        /// <param name="minUpDot">윗면으로 칠 법선과 위쪽 방향의 최소 내적(0~1). 1에 가까울수록 엄격하다.</param>
        /// <returns>(du/ds, dv/ds) 단위 1/m. 윗면 정점이 둘 미만이거나 흐름 방향으로 위치가 퍼져 있지 않으면 잴 수 없으므로 0이다.</returns>
        public static Vector2 PerMeter(
            IReadOnlyList<Vector3> worldPositions,
            IReadOnlyList<Vector3> worldNormals,
            IReadOnlyList<Vector2> uvs,
            Vector3 flowDirection,
            float minUpDot)
        {
            if (flowDirection.sqrMagnitude < Mathf.Epsilon || worldPositions.Count != uvs.Count || worldNormals.Count != uvs.Count)
            {
                return Vector2.zero;
            }

            Vector3 flow = flowDirection.normalized;
            int count = 0;
            float meanS = 0f;
            Vector2 meanUv = Vector2.zero;
            for (int i = 0; i < uvs.Count; i++)
            {
                if (!IsTop(worldNormals[i], minUpDot))
                {
                    continue;
                }

                meanS += Vector3.Dot(worldPositions[i], flow);
                meanUv += uvs[i];
                count++;
            }

            if (count < 2)
            {
                return Vector2.zero;
            }

            meanS /= count;
            meanUv /= count;

            float varS = 0f;
            Vector2 cov = Vector2.zero;
            for (int i = 0; i < uvs.Count; i++)
            {
                if (!IsTop(worldNormals[i], minUpDot))
                {
                    continue;
                }

                float ds = Vector3.Dot(worldPositions[i], flow) - meanS;
                varS += ds * ds;
                cov += (uvs[i] - meanUv) * ds;
            }

            return varS > Mathf.Epsilon ? cov / varS : Vector2.zero;
        }

        private static bool IsTop(Vector3 worldNormal, float minUpDot)
        {
            return Vector3.Dot(worldNormal.normalized, Vector3.up) >= minUpDot;
        }
    }
}
