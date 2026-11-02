using System;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 알려 준 관절 각도로 물건을 집으러 갈 때 거치는 관절 각도들: 물건 앞(Near), 쥐는 자리(Grasp), 쥔 뒤 뺀 자리(Retreat).
    /// 쥐는 자리는 알려 준 관절 모양에서 출발해 풀므로, 서는 자리가 알려 준 자리와 같으면 알려 준 관절 각도와 사실상 같고
    /// 조금 어긋났어도 같은 모양으로 알려 준 도구 끝점 자세에 닿는다. 앞뒤 자리는 쥐는 자리의 해에서 출발해 풀어 모양이 이어진다.
    /// 풀이 자체는 주입받으므로 씬 없이 시험한다.
    /// </summary>
    public sealed class TaughtJointRoute
    {
        private TaughtJointRoute(float[] near, float[] grasp, float[] retreat)
        {
            Near = near;
            Grasp = grasp;
            Retreat = retreat;
        }

        public float[] Near { get; }

        public float[] Grasp { get; }

        public float[] Retreat { get; }

        /// <param name="taughtRad">알려 준 관절 각도(rad). 풀이의 출발 자세다.</param>
        /// <param name="solveFrom">(도구 끝점 월드 자세, 출발 관절 각도)로 닿는 관절 각도를 돌려준다. 못 닿으면 null.</param>
        /// <returns>세 자리 모두 풀렸으면 true. 하나라도 못 풀면 false이고 route는 null이다.</returns>
        public static bool TryBuild(
            IReadOnlyList<float> taughtRad,
            Pose near,
            Pose grasp,
            Pose retreat,
            Func<Pose, IReadOnlyList<float>, float[]> solveFrom,
            out TaughtJointRoute route)
        {
            if (taughtRad == null)
            {
                throw new ArgumentNullException(nameof(taughtRad));
            }

            if (solveFrom == null)
            {
                throw new ArgumentNullException(nameof(solveFrom));
            }

            route = null;

            float[] graspRad = solveFrom(grasp, taughtRad);
            if (graspRad == null)
            {
                return false;
            }

            float[] nearRad = solveFrom(near, graspRad);
            float[] retreatRad = solveFrom(retreat, graspRad);
            if (nearRad == null || retreatRad == null)
            {
                return false;
            }

            route = new TaughtJointRoute(nearRad, graspRad, retreatRad);
            return true;
        }
    }
}
