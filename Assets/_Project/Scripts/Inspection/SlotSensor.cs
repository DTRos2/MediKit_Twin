using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Inspection
{
    /// <summary>
    /// 박스 슬롯 하나의 감지 센서. 트리거 콜라이더가 슬롯 부피를 정하고, 그 안에 있는 구성품(KitItemLabel)의 ID를 읽는다. (FR-06)
    /// 박스 프리팹에 두며 콜라이더는 레시피의 슬롯 좌표와 같은 자리에 놓는다.
    /// 선반 물건의 복제본은 콜라이더가 없어 물리 트리거 이벤트가 오지 않으므로, 읽을 때 위치가 부피 안인지 직접 확인한다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SlotSensor : MonoBehaviour
    {
        [Tooltip("레시피의 슬롯 번호(1부터). 판정의 MissingSlots와 같은 번호다.")]
        [SerializeField, Min(1)] private int _slotNumber = 1;

        [Tooltip("끄면 센서가 응답하지 않는 상황(FR-06 예외)을 흉내 낸다.")]
        [SerializeField] private bool _isResponsive = true;

        private BoxCollider _volume;

        public int SlotNumber => _slotNumber;

        public bool IsResponsive
        {
            get => _isResponsive;
            set => _isResponsive = value;
        }

        /// <summary>에디터가 박스 프리팹에 센서를 만들 때 부른다.</summary>
        public void Configure(int slotNumber)
        {
            _slotNumber = slotNumber;
        }

        /// <summary>
        /// 슬롯 부피 안에 있는 구성품의 ID. 비어 있으면 null이다. 여러 개가 겹치면 부피 중심에 가장 가까운 것을 읽는다.
        /// </summary>
        public string Read()
        {
            if (_volume == null)
            {
                _volume = GetComponent<BoxCollider>();
            }

            string found = null;
            float nearest = float.MaxValue;

            foreach (KitItemLabel label in FindObjectsByType<KitItemLabel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Vector3 local = transform.InverseTransformPoint(label.transform.position) - _volume.center;
                Vector3 half = _volume.size * 0.5f;
                bool inside = Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
                if (inside && local.sqrMagnitude < nearest)
                {
                    nearest = local.sqrMagnitude;
                    found = label.ComponentId;
                }
            }

            return found;
        }

        private void Reset()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }
    }
}
