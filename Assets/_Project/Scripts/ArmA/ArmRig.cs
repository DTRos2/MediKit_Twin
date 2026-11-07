using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>프로필에 적힌 이름으로 씬의 로봇에서 찾아낸 링크 묶음. ArmController와 에디터 도구가 같은 방식으로 링크를 찾는다.</summary>
    public sealed class ArmRig
    {
        public Transform Base { get; }
        public Transform Tool { get; }
        public ArticulationBody[] Joints { get; }

        private ArmRig(Transform baseLink, Transform tool, ArticulationBody[] joints)
        {
            Base = baseLink;
            Tool = tool;
            Joints = joints;
        }

        /// <summary>링크를 모두 찾으면 true. 못 찾으면 error에 이유를 담는다.</summary>
        public static bool TryResolve(Transform robotRoot, ArmProfileSO profile, out ArmRig rig, out string error)
        {
            rig = null;
            error = null;

            Transform baseLink = TransformSearch.FindDeep(robotRoot, profile.BaseLinkName);
            Transform tool = TransformSearch.FindDeep(robotRoot, profile.ToolLinkName);
            if (baseLink == null || tool == null)
            {
                error = $"링크를 찾지 못했다. base='{profile.BaseLinkName}', tool='{profile.ToolLinkName}'";
                return false;
            }

            ArmProfileSO.JointEntry[] entries = profile.Joints;
            var joints = new ArticulationBody[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                Transform link = TransformSearch.FindDeep(robotRoot, entries[i].LinkName);
                joints[i] = link != null ? link.GetComponent<ArticulationBody>() : null;
                if (joints[i] == null)
                {
                    error = $"관절 링크 '{entries[i].LinkName}'(관절 {entries[i].JointName})의 ArticulationBody를 찾지 못했다.";
                    return false;
                }
            }

            rig = new ArmRig(baseLink, tool, joints);
            return true;
        }
    }
}
