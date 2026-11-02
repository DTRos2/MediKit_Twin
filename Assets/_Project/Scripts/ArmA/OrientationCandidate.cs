using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 탐색할 도구 방향 하나. 기본 방향에서 기울이기, 좌우 돌리기, 손목 비틀기를 얼마나 했는지와, 그 방향이 얼마나 무리한지(DeviationDeg)를 함께 가진다.
    /// 무리한 정도는 기울이기, 좌우, 비틀기를 제곱합 제곱근으로 합치되 비틀기는 가중치를 곱해 줄인다.
    /// 손목 비틀기는 칸 안으로 들어가는 방향을 바꾸지 않아 선반 칸 틀에 걸릴 위험이 거의 없지만, 기울이기와 좌우 돌리기는 방향이 틀어져 걸리기 쉽기 때문이다.
    /// 회전축은 모두 기본 방향 기준의 로컬 축이다(기울이기=오른쪽 축, 좌우=위쪽 축, 비틀기=앞쪽 축). 순수 계산이라 씬 없이 시험한다.
    /// </summary>
    public readonly struct OrientationCandidate
    {
        private OrientationCandidate(Quaternion rotation, float pitchDeg, float yawDeg, float rollDeg, float deviationDeg)
        {
            Rotation = rotation;
            PitchDeg = pitchDeg;
            YawDeg = yawDeg;
            RollDeg = rollDeg;
            DeviationDeg = deviationDeg;
        }

        /// <summary>월드 기준 도구 방향.</summary>
        public Quaternion Rotation { get; }

        /// <summary>기울인 각도(도). 도구 끝이 아래로 향하면 양수.</summary>
        public float PitchDeg { get; }

        /// <summary>좌우로 돌린 각도(도).</summary>
        public float YawDeg { get; }

        /// <summary>손목을 비튼 각도(도).</summary>
        public float RollDeg { get; }

        /// <summary>기본 방향에서 벌어진 정도(도). 비틀기는 가중치만큼 줄여 센다. 0이면 기본 방향이다.</summary>
        public float DeviationDeg { get; }

        /// <param name="rollCostWeight">비틀기 1도를 기울이기·좌우 몇 도로 칠지. 1이면 같게, 0이면 비틀기는 공짜로 센다.</param>
        public static OrientationCandidate Create(Quaternion nominal, float pitchDeg, float yawDeg, float rollDeg, float rollCostWeight)
        {
            Quaternion rotation = nominal
                * Quaternion.AngleAxis(yawDeg, Vector3.up)
                * Quaternion.AngleAxis(pitchDeg, Vector3.right)
                * Quaternion.AngleAxis(rollDeg, Vector3.forward);

            float weightedRoll = rollDeg * rollCostWeight;
            float deviation = Mathf.Sqrt(pitchDeg * pitchDeg + yawDeg * yawDeg + weightedRoll * weightedRoll);
            return new OrientationCandidate(rotation, pitchDeg, yawDeg, rollDeg, deviation);
        }
    }
}
