using UnityEngine;

namespace MediKitTwin.Mobile
{
    public enum DriveAction
    {
        /// <summary>제자리에서 돈다. YawDeg가 이번 프레임 뒤의 방향이다.</summary>
        Turn,

        /// <summary>목표 쪽으로 나아간다. Translation만큼 옮긴다.</summary>
        Move,

        /// <summary>도착했다. SnapsYaw이면 YawDeg로 방향을 정확히 맞춘다.</summary>
        Arrive,
    }

    /// <summary>
    /// 차동 구동이 한 프레임에 하기로 한 일. 구동 판단(DifferentialDrive)은 이것만 돌려주고, 실제로 옮기는 일은 부른 쪽이 한다.
    /// 그래서 판단을 씬 없이 시험할 수 있다.
    /// </summary>
    public readonly struct DriveStep
    {
        private DriveStep(DriveAction action, float yawDeg, Vector3 translation, bool snapsYaw)
        {
            Action = action;
            YawDeg = yawDeg;
            Translation = translation;
            SnapsYaw = snapsYaw;
        }

        public DriveAction Action { get; }

        public float YawDeg { get; }

        public Vector3 Translation { get; }

        public bool SnapsYaw { get; }

        public static DriveStep Turn(float yawDeg)
        {
            return new DriveStep(DriveAction.Turn, yawDeg, Vector3.zero, false);
        }

        public static DriveStep Move(Vector3 translation)
        {
            return new DriveStep(DriveAction.Move, 0f, translation, false);
        }

        public static DriveStep Arrive()
        {
            return new DriveStep(DriveAction.Arrive, 0f, Vector3.zero, false);
        }

        public static DriveStep ArriveAndSnapYaw(float yawDeg)
        {
            return new DriveStep(DriveAction.Arrive, yawDeg, Vector3.zero, true);
        }
    }
}
