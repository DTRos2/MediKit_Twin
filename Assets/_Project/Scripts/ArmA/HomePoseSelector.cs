using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 프로필의 시작 자세를 고른다. 프로필 각도는 URDF 기준인데 Unity가 관절 부호를 반대로 만들 수 있으므로,
    /// 부호를 그대로 쓴 자세와 뒤집은 자세 중 도구가 더 아래를 향하는 쪽을 순기구학으로 선택한다.
    /// 에디터에서 링크를 돌려 두는 도구와 Play 중 ArmController가 같은 결과를 내도록 한 곳에 둔다.
    /// </summary>
    public static class HomePoseSelector
    {
        public static float[] Choose(ArmProfileSO profile, KinematicChain chain, Transform baseLink)
        {
            float[] pose = profile.HomePoseRad;
            if (!profile.HasHomePose)
            {
                return pose;
            }

            var flipped = new float[pose.Length];
            for (int i = 0; i < pose.Length; i++)
            {
                flipped[i] = -pose[i];
            }

            return DownwardScore(chain, baseLink, flipped) > DownwardScore(chain, baseLink, pose) ? flipped : pose;
        }

        // 도구 접근 방향(+Z)이 월드 아래를 향할수록 크다.
        private static float DownwardScore(KinematicChain chain, Transform baseLink, float[] anglesRad)
        {
            Quaternion toolRotation = baseLink.rotation * chain.Forward(anglesRad).rotation;
            return -(toolRotation * Vector3.forward).y;
        }
    }
}
