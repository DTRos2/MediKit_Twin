using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 관절마다 이번 스텝에 옮길 각도를 정한다. 가장 많이 움직여야 하는 관절이 최대 속도로 움직이고, 나머지는 같은 시각에 도착하도록 그 비율만큼만 움직인다.
    /// 관절마다 따로 최대 속도로 가면 조금만 움직이는 관절이 먼저 도착해, 도구 끝점이 직선이 아닌 휘어진 길로 간다.
    /// 선반 칸 안으로 들어갈 때 그 휘어짐이 아래 판에 손가락을 박는 일이 있어, 관절 공간에서 직선(동시 도착)으로 가게 한다.
    /// 전체 이동 시간은 그대로이고(가장 먼 관절이 정한다) 길 모양만 달라진다. 순수 계산이라 씬 없이 시험한다.
    /// </summary>
    public static class SynchronizedJointStep
    {
        /// <param name="command">관절마다 지금 명령(rad).</param>
        /// <param name="goal">관절마다 목표(rad). 한계 안으로 이미 맞춘 값이다.</param>
        /// <param name="maxStepRad">가장 많이 움직이는 관절이 이번에 움직일 수 있는 최대 각도(rad).</param>
        /// <param name="stepsRad">결과: 관절마다 이번에 옮길 크기(rad, 0 이상). 방향은 부르는 쪽이 목표를 보고 정한다.</param>
        public static void Compute(float[] command, float[] goal, float maxStepRad, float[] stepsRad)
        {
            if (command == null || goal == null || stepsRad == null)
            {
                throw new ArgumentNullException(command == null ? nameof(command) : goal == null ? nameof(goal) : nameof(stepsRad));
            }

            if (goal.Length != command.Length || stepsRad.Length != command.Length)
            {
                throw new ArgumentException("관절 수가 서로 맞지 않는다.");
            }

            float largest = 0f;
            for (int i = 0; i < command.Length; i++)
            {
                largest = Mathf.Max(largest, Mathf.Abs(goal[i] - command[i]));
            }

            // 가장 먼 관절이 이번 스텝에 도착할 수 있으면 모두 도착한다. 아니면 먼 정도에 비례해 나눈다.
            float scale = largest <= maxStepRad || largest <= 0f ? 1f : maxStepRad / largest;
            for (int i = 0; i < command.Length; i++)
            {
                stepsRad[i] = Mathf.Abs(goal[i] - command[i]) * scale;
            }
        }
    }
}
