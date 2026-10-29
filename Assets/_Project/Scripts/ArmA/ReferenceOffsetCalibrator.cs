using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 에디터에서 링크를 돌려 둔 로봇을 Unity가 어떻게 읽었는지 판별해, URDF 각도와 ArticulationBody 값의 기준 차이(offset)를 구한다.
    /// 돌려 둔 자세를 그대로 URDF 각도로 읽었으면 offset 0, 그 자세를 0도 기준으로 잡았으면 offset = 돌려 둔 각도다.
    /// 첫 물리 스텝이 지난 뒤에 불러야 관절 값을 믿을 수 있다.
    /// </summary>
    public static class ReferenceOffsetCalibrator
    {
        private const float ToleranceRad = 0.05f;

        /// <param name="editorPoseRad">에디터 도구가 기록한 돌려 둔 자세(URDF 기준). 비었거나 관절 수와 다르면 보정하지 않는다.</param>
        /// <param name="warning">어느 쪽으로도 판별하지 못했을 때의 이유. 문제가 없으면 null.</param>
        public static float[] Calibrate(ArticulationBody[] joints, float[] editorPoseRad, out string warning)
        {
            warning = null;
            var offsets = new float[joints.Length];
            if (editorPoseRad == null || editorPoseRad.Length != joints.Length)
            {
                return offsets;
            }

            var raw = new float[joints.Length];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = joints[i].jointPosition[0];
            }

            if (MaxAbsDifference(raw, editorPoseRad) <= ToleranceRad)
            {
                return offsets; // 돌려 둔 자세를 그대로 URDF 각도로 읽었다.
            }

            if (MaxAbsDifference(raw, null) <= ToleranceRad)
            {
                // 돌려 둔 자세가 0도 기준이 되었다. URDF 각도 = 읽은 값 + 돌려 둔 각도.
                System.Array.Copy(editorPoseRad, offsets, offsets.Length);
                ShiftDriveLimits(joints, offsets);
                return offsets;
            }

            warning = "편집 모드 자세와 첫 관절 각도가 어느 쪽과도 맞지 않아 각도 기준을 보정하지 못했다.";
            return offsets;
        }

        // 기준이 밀리면 Unity의 관절 한계도 새 0도 기준으로 해석되므로, URDF 한계에서 offset만큼 빼서 다시 쓴다.
        private static void ShiftDriveLimits(ArticulationBody[] joints, float[] offsetsRad)
        {
            for (int i = 0; i < joints.Length; i++)
            {
                if (!ArticulationJointLimits.IsLimited(joints[i]))
                {
                    continue;
                }

                ArticulationDrive drive = joints[i].xDrive;
                drive.lowerLimit -= offsetsRad[i] * Mathf.Rad2Deg;
                drive.upperLimit -= offsetsRad[i] * Mathf.Rad2Deg;
                joints[i].xDrive = drive;
            }
        }

        // reference가 null이면 0과 비교한다.
        private static float MaxAbsDifference(float[] values, float[] reference)
        {
            float largest = 0f;
            for (int i = 0; i < values.Length; i++)
            {
                largest = Mathf.Max(largest, Mathf.Abs(values[i] - (reference != null ? reference[i] : 0f)));
            }

            return largest;
        }
    }
}
