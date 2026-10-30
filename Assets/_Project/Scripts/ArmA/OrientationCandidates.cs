using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 기본 도구 방향에서 조금씩 기울이고 돌리고 비튼 후보 방향들을 만든다. 기본 방향에서 가까운 것부터 정렬하므로
    /// 앞에서부터 닿는 것을 찾으면 그것이 기본에 가장 가까운(가장 덜 무리한) 방향이다. 순수 계산이라 씬 없이 시험한다.
    /// 회전축은 모두 기본 방향 기준의 로컬 축이다(기울이기=오른쪽 축, 좌우=위쪽 축, 비틀기=앞쪽 축).
    /// </summary>
    public static class OrientationCandidates
    {
        public static IReadOnlyList<Quaternion> Generate(Quaternion nominal, OrientationSearchSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            float[] pitches = Steps(settings.MaxPitchDownDeg, settings.MaxPitchUpDeg, settings.PitchStepDeg);
            float[] yaws = Steps(settings.MaxYawDeg, settings.MaxYawDeg, settings.YawStepDeg);
            float[] rolls = Steps(settings.MaxRollDeg, settings.MaxRollDeg, settings.RollStepDeg);

            var candidates = new List<(Quaternion rotation, float deviation)>();
            foreach (float pitch in pitches)
            {
                foreach (float yaw in yaws)
                {
                    foreach (float roll in rolls)
                    {
                        Quaternion rotation = nominal
                            * Quaternion.AngleAxis(yaw, Vector3.up)
                            * Quaternion.AngleAxis(pitch, Vector3.right)
                            * Quaternion.AngleAxis(roll, Vector3.forward);
                        candidates.Add((rotation, Mathf.Sqrt(pitch * pitch + yaw * yaw + roll * roll)));
                    }
                }
            }

            // OrderBy는 안정 정렬이라 같은 편차이면 만든 순서(기울이기, 좌우, 비틀기 순)를 지킨다.
            return candidates.OrderBy(candidate => candidate.deviation).Select(candidate => candidate.rotation).ToList();
        }

        // 0과, 간격의 배수 중 양쪽 한계 안에 드는 값들. 간격이 0이면 0만 있다.
        private static float[] Steps(float maxPositive, float maxNegative, float step)
        {
            var values = new List<float> { 0f };
            if (step <= 0f)
            {
                return values.ToArray();
            }

            for (float value = step; value <= maxPositive + 1e-4f; value += step)
            {
                values.Add(value);
            }

            for (float value = -step; value >= -maxNegative - 1e-4f; value -= step)
            {
                values.Add(value);
            }

            return values.ToArray();
        }
    }
}
