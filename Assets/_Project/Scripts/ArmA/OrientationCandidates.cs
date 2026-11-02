using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 기본 도구 방향에서 조금씩 기울이고 돌리고 비튼 후보 방향들을 만든다. 덜 무리한 것부터 정렬하므로
    /// 앞에서부터 닿는 것을 찾으면 그것이 가장 덜 무리한 방향이다. 무리한 정도는 OrientationCandidate가 센다:
    /// 기울이기와 좌우 돌리기는 그대로, 손목 비틀기는 접근 방향을 바꾸지 않으므로 가중치만큼 줄여서 센다.
    /// 순수 계산이라 씬 없이 시험한다.
    /// </summary>
    public static class OrientationCandidates
    {
        /// <summary>후보 방향들을 덜 무리한 순서로 돌려준다.</summary>
        public static IReadOnlyList<Quaternion> Generate(Quaternion nominal, OrientationSearchSettings settings)
        {
            return GenerateRanked(nominal, settings).Select(candidate => candidate.Rotation).ToList();
        }

        /// <summary>후보와 각 후보가 무리한 정도를 덜 무리한 순서로 돌려준다.</summary>
        public static IReadOnlyList<OrientationCandidate> GenerateRanked(Quaternion nominal, OrientationSearchSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            float[] pitches = Steps(settings.MaxPitchDownDeg, settings.MaxPitchUpDeg, settings.PitchStepDeg);
            float[] yaws = Steps(settings.MaxYawDeg, settings.MaxYawDeg, settings.YawStepDeg);
            float[] rolls = Steps(settings.MaxRollDeg, settings.MaxRollDeg, settings.RollStepDeg);

            var candidates = new List<OrientationCandidate>();
            foreach (float pitch in pitches)
            {
                foreach (float yaw in yaws)
                {
                    foreach (float roll in rolls)
                    {
                        candidates.Add(OrientationCandidate.Create(nominal, pitch, yaw, roll, settings.RollCostWeight));
                    }
                }
            }

            // OrderBy는 안정 정렬이라 같은 정도이면 만든 순서(기울이기, 좌우, 비틀기 순)를 지킨다.
            return candidates.OrderBy(candidate => candidate.DeviationDeg).ToList();
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
