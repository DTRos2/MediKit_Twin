using UnityEngine;

namespace MediKitTwin.Common
{
    /// <summary>
    /// 구성품 오브젝트가 어떤 종류인지 알리는 표시. 선반 물건의 복제본이 박스에 놓여도 이 표시가 따라가므로
    /// 슬롯 감지 센서가 이름을 해석하지 않고 구성품 ID를 읽는다. (FR-06)
    /// </summary>
    public sealed class KitComponentTag : MonoBehaviour
    {
        [Tooltip("구성품 종류 ID. 레시피의 ComponentId와 같다.")]
        [SerializeField] private string _componentId;

        public string ComponentId => _componentId;

        /// <summary>에디터가 선반 물건을 만들 때 부른다.</summary>
        public void Configure(string componentId)
        {
            _componentId = componentId;
        }
    }
}
