using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>두 관절 자세 사이에서 관절이 얼마나 돌아야 하는지를 재는 도구.</summary>
    public static class JointTravel
    {
        /// <summary>fromRad에서 toRad로 가려면 가장 많이 돌아야 하는 관절의 각도(rad). 관절 수는 fromRad를 따른다.</summary>
        public static float Largest(float[] fromRad, float[] toRad)
        {
            float largest = 0f;
            for (int i = 0; i < fromRad.Length; i++)
            {
                largest = Mathf.Max(largest, Mathf.Abs(toRad[i] - fromRad[i]));
            }

            return largest;
        }
    }
}
