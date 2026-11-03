using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Label
{
    /// <summary>
    /// 뚜껑에 붙은 라벨 오브젝트의 표시. 어느 키트의 라벨인지와 적힌 글을 들고 있다. (FR-11)
    /// 시험과 대시보드가 라벨이 붙었는지, 무엇이 적혔는지를 오브젝트 이름이 아니라 이 컴포넌트로 읽는다.
    /// </summary>
    public sealed class KitLabelTag : MonoBehaviour
    {
        public KitData Kit { get; private set; }

        /// <summary>라벨에 적힌 줄들.</summary>
        public string[] Lines { get; private set; }

        public void Configure(KitData kit, string[] lines)
        {
            Kit = kit;
            Lines = lines;
        }
    }
}
