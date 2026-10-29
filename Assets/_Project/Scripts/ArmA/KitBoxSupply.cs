using System.Collections;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 키팅 지그 위에 키트 박스가 없으면 새 박스를 자동으로 놓는다. (FR-03의 박스 공급)
    /// 박스가 지그 자리에서 occupiedRadius보다 멀어지거나 사라지면 "없다"고 보고,
    /// respawnDelay가 지난 뒤 위에서 내려오게 새 박스를 만든다.
    /// 자식 KitBox가 처음 놓인 자리이자 새 박스의 원본이다. 시간은 시뮬레이션 시간(Time.deltaTime)이다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KitBoxSupply : MonoBehaviour
    {
        private const string BoxName = "KitBox";

        [Tooltip("박스가 없어진 뒤 새 박스가 생기기까지의 시간(초)")]
        [SerializeField] private float respawnDelay = 2f;

        [Tooltip("박스가 이 거리 안에 있으면 지그 위에 있다고 본다(m)")]
        [SerializeField] private float occupiedRadius = 0.15f;

        [Header("생성 연출")]
        [Tooltip("새 박스가 내려오기 시작하는 높이(m)")]
        [SerializeField] private float dropHeight = 0.3f;
        [Tooltip("새 박스가 내려오는 데 걸리는 시간(초)")]
        [SerializeField] private float dropSeconds = 0.6f;

        [Header("테스트 (Remove Box)")]
        [Tooltip("치운 박스를 들어 올리는 높이(m)")]
        [SerializeField] private float testLiftHeight = 0.4f;
        [Tooltip("치운 박스가 올라가서 사라지기까지의 시간(초)")]
        [SerializeField] private float testLiftSeconds = 1.5f;

        private Transform template;
        private Vector3 slotLocalPosition;
        private Quaternion slotLocalRotation;

        private Transform current;
        private float emptySeconds;
        private bool dropping;

        /// <summary>지그 위에 박스가 있으면 true. 내려오는 중인 박스도 있는 것으로 본다.</summary>
        public bool HasBox => dropping || IsOnJig(current);

        /// <summary>지그 위 박스. 없으면 null이다.</summary>
        public Transform CurrentBox => HasBox ? current : null;

        private void Awake()
        {
            Transform box = transform.Find(BoxName);
            if (box == null)
            {
                Debug.LogError($"[KitBoxSupply] {name} 아래에 {BoxName}이(가) 없습니다.", this);
                enabled = false;
                return;
            }

            slotLocalPosition = box.localPosition;
            slotLocalRotation = box.localRotation;
            current = box;

            // 처음 박스를 복사해 둔 원본. 꺼 두어서 보이지도 충돌하지도 않는다.
            template = Instantiate(box, transform);
            template.name = BoxName + "Template";
            template.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (dropping) return;

            if (IsOnJig(current))
            {
                emptySeconds = 0f;
                return;
            }

            current = null;
            emptySeconds += Time.deltaTime;
            if (emptySeconds >= respawnDelay)
            {
                emptySeconds = 0f;
                StartCoroutine(DropNewBox());
            }
        }

        private bool IsOnJig(Transform box)
        {
            if (box == null) return false;

            Vector3 slotWorld = transform.TransformPoint(slotLocalPosition);
            return (box.position - slotWorld).sqrMagnitude <= occupiedRadius * occupiedRadius;
        }

        private IEnumerator DropNewBox()
        {
            dropping = true;

            Transform box = Instantiate(template, transform);
            box.name = BoxName;
            box.localRotation = slotLocalRotation;
            box.gameObject.SetActive(true);
            current = box;

            Vector3 start = slotLocalPosition + Vector3.up * dropHeight;
            for (float t = 0f; t < dropSeconds; t += Time.deltaTime)
            {
                box.localPosition = Vector3.Lerp(start, slotLocalPosition, t / dropSeconds);
                yield return null;
            }

            box.localPosition = slotLocalPosition;
            dropping = false;
        }

        // 컨베이어로 보내는 동작이 없는 지금은 인스펙터에서 직접 눌러 확인한다. (플레이 모드에서 컴포넌트 우클릭)
        // 박스를 위로 들어 올렸다가 지우면, 잠시 뒤 새 박스가 위에서 내려온다.
        [ContextMenu("Remove Box (Test)")]
        private void RemoveBoxForTest()
        {
            if (!HasBox || dropping) return;

            Transform box = current;
            current = null;
            StartCoroutine(LiftAndDiscard(box));
        }

        private IEnumerator LiftAndDiscard(Transform box)
        {
            // 부모를 떼어 월드 기준으로 올린다. (지그 자리를 벗어나므로 "없다"로 판단된다)
            box.SetParent(null, true);
            Vector3 start = box.position;
            Vector3 end = start + Vector3.up * testLiftHeight;
            for (float t = 0f; t < testLiftSeconds; t += Time.deltaTime)
            {
                box.position = Vector3.Lerp(start, end, t / testLiftSeconds);
                yield return null;
            }

            Destroy(box.gameObject);
        }
    }
}
