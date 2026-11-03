using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 벨트 무늬가 흐르는 방향과 텍스처의 무늬가 바뀌는 방향을 맞추는 계산.
    /// 벨트 텍스처는 한 축으로만 줄무늬가 있어서(예: U 방향), 구간이 무늬가 없는 축(V)으로 흐르면 UV 오프셋을 아무리 밀어도 보이는 변화가 없다.
    /// 구간마다 메시의 UV가 놓인 방향이 달라서(같은 큐브라도 코너는 입구가 다른 면에 있다) 이런 구간이 생긴다.
    /// 그런 구간은 메시의 U와 V를 맞바꾸면 흐름이 무늬 축을 따르게 된다. 순수 계산이라 씬 없이 시험한다.
    /// </summary>
    public static class BeltUvAxes
    {
        /// <summary>
        /// 구간이 무늬가 바뀌는 축(patternAxis)이 아닌 축으로 더 많이 흐르면 true. 이때는 오프셋을 밀어도 무늬가 움직이는 것이 보이지 않는다.
        /// </summary>
        /// <param name="uvPerMeter">흐름 방향 1m당 (u, v)가 바뀌는 양(BeltUvFlow.PerMeter).</param>
        public static bool IsAcrossPattern(Vector2 uvPerMeter, UvAxis patternAxis)
        {
            float along = Mathf.Abs(patternAxis == UvAxis.U ? uvPerMeter.x : uvPerMeter.y);
            float across = Mathf.Abs(patternAxis == UvAxis.U ? uvPerMeter.y : uvPerMeter.x);
            return across > along;
        }

        /// <summary>U와 V를 맞바꾼 좌표.</summary>
        public static Vector2 Swapped(Vector2 uv)
        {
            return new Vector2(uv.y, uv.x);
        }

        /// <summary>모든 좌표의 U와 V를 맞바꾼 새 배열. 원본은 바꾸지 않는다.</summary>
        public static Vector2[] Swapped(IReadOnlyList<Vector2> uvs)
        {
            var swapped = new Vector2[uvs.Count];
            for (int i = 0; i < uvs.Count; i++)
            {
                swapped[i] = Swapped(uvs[i]);
            }

            return swapped;
        }
    }
}
