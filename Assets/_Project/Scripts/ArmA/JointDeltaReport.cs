using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// IK가 현재 자세에서 해까지 관절마다 몇 도를 움직이라고 했는지 한 줄로 만든다.
    /// 어느 관절이 실제로 쓰이고 어느 관절이 놀고 있는지를 실행 중에 확인하는 데 쓴다. 순수 문자열 계산이라 씬 없이 시험한다.
    /// </summary>
    public static class JointDeltaReport
    {
        /// <summary>예: "j1 +12.0° / j2 -34.5° / j3 0.0° ..." 각도는 현재 자세에서 목표까지의 변화량이다.</summary>
        public static string Format(IReadOnlyList<string> jointNames, IReadOnlyList<float> fromRad, IReadOnlyList<float> toRad)
        {
            var text = new StringBuilder();
            for (int i = 0; i < jointNames.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(" / ");
                }

                float deltaDeg = (toRad[i] - fromRad[i]) * Mathf.Rad2Deg;
                text.Append(jointNames[i]).Append(' ').Append(deltaDeg.ToString("+0.0;-0.0;0.0")).Append('°');
            }

            return text.ToString();
        }
    }
}
