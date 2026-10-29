using UnityEngine;

namespace MediKitTwin.Rl
{
    /// <summary>한 스텝의 보상 계산에 필요한 값 묶음. 보상 함수는 이 값만 읽는다.</summary>
    public readonly struct RlContext
    {
        public readonly Vector3 ToolPosition;
        public readonly Vector3 GoalPosition;
        public readonly float Distance;
        public readonly float PreviousDistance;
        public readonly float ActionMagnitude;
        public readonly bool GripperHolding;
        public readonly int Step;

        public RlContext(Vector3 toolPosition, Vector3 goalPosition, float previousDistance, float actionMagnitude, bool gripperHolding, int step)
        {
            ToolPosition = toolPosition;
            GoalPosition = goalPosition;
            Distance = Vector3.Distance(toolPosition, goalPosition);
            PreviousDistance = previousDistance;
            ActionMagnitude = actionMagnitude;
            GripperHolding = gripperHolding;
            Step = step;
        }
    }
}
