using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmB
{
    /// <summary>
    /// 뚜껑 박스에 쌓인 뚜껑을 관리한다. 맨 위 뚜껑을 가져가면 나머지 뚜껑이 한 칸씩 올라오고,
    /// 맨 아래에 새 뚜껑이 아래에서 올라와서 박스는 항상 가득 찬 채로 유지된다.
    /// 맨 위 뚜껑은 항상 같은 높이(로봇암 B가 잡는 위치)에 놓인다.
    /// 뚜껑은 자식 오브젝트 Lid_01(맨 위), Lid_02, ... 순서로 찾는다.
    /// 올라오는 시간은 시뮬레이션 시간(Time.deltaTime)이므로 배속과 일시정지가 반영된다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LidDispenser : MonoBehaviour
    {
        private const string LidNamePrefix = "Lid_";

        [Tooltip("뚜껑이 한 칸 올라오는 데 걸리는 시간(초)")]
        [SerializeField] private float riseDuration = 1.5f;

        [Header("테스트 (Take Top Lid)")]
        [Tooltip("가져간 뚜껑을 들어 올리는 높이(m)")]
        [SerializeField] private float testLiftHeight = 0.4f;
        [Tooltip("가져간 뚜껑이 올라가서 사라지기까지의 시간(초)")]
        [SerializeField] private float testLiftSeconds = 1.5f;

        // 위에서부터 차례로. 0번이 로봇암이 잡는 뚜껑이다.
        private readonly List<Transform> stack = new List<Transform>();
        private Vector3[] slotLocalPositions;
        private Vector3 slotStep;
        private float riseSpeed;

        /// <summary>뚜껑이 올라오는 중이면 true. 이때는 새 뚜껑을 가져갈 수 없다.</summary>
        public bool IsBusy { get; private set; }

        /// <summary>맨 위 뚜껑이 놓이는 위치(월드). 로봇암 B의 집는 목표가 된다.</summary>
        public Vector3 TopLidWorldPosition => transform.TransformPoint(slotLocalPositions[0]);

        private void Awake()
        {
            CollectLids();
            if (stack.Count == 0)
            {
                Debug.LogError($"[LidDispenser] {name} 아래에 {LidNamePrefix}01 같은 뚜껑이 없습니다.", this);
                enabled = false;
                return;
            }

            // 처음 놓인 자리를 칸 위치로 기억한다.
            slotLocalPositions = new Vector3[stack.Count];
            for (int i = 0; i < stack.Count; i++) slotLocalPositions[i] = stack[i].localPosition;

            // 한 칸 아래로 내려가는 벡터. 뚜껑이 하나뿐이면 4cm로 본다.
            slotStep = stack.Count > 1 ? slotLocalPositions[1] - slotLocalPositions[0] : Vector3.down * 0.04f;
            riseSpeed = slotStep.magnitude / Mathf.Max(riseDuration, 0.01f);
        }

        /// <summary>
        /// 맨 위 뚜껑을 가져간다. 뚜껑은 이 박스에서 떼어져 호출한 쪽이 갖고, 아래에서 새 뚜껑이 올라온다.
        /// 올라오는 중이면 false이다.
        /// </summary>
        public bool TryTakeTopLid(out Transform lid)
        {
            lid = null;
            if (IsBusy || stack.Count == 0) return false;

            lid = stack[0];
            stack.RemoveAt(0);

            // 새 뚜껑은 맨 아래 칸보다 한 칸 아래에서 시작해 올라온다.
            Transform lowest = stack.Count > 0 ? stack[stack.Count - 1] : lid;
            Transform fresh = Instantiate(lid, transform);
            fresh.localPosition = lowest.localPosition + slotStep;
            fresh.localRotation = lid.localRotation;
            stack.Add(fresh);

            // 가져간 뚜껑은 박스에서 떼되 월드 위치는 유지한다.
            lid.SetParent(null, true);

            RenameLids();
            IsBusy = true;
            return true;
        }

        private void Update()
        {
            if (!IsBusy) return;

            bool arrived = true;
            for (int i = 0; i < stack.Count; i++)
            {
                Transform lid = stack[i];
                lid.localPosition = Vector3.MoveTowards(lid.localPosition, slotLocalPositions[i], riseSpeed * Time.deltaTime);
                if ((lid.localPosition - slotLocalPositions[i]).sqrMagnitude > 1e-8f) arrived = false;
            }

            if (arrived) IsBusy = false;
        }

        private void CollectLids()
        {
            stack.Clear();
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith(LidNamePrefix)) stack.Add(child);
            }

            // 이름 순서(Lid_01이 맨 위)로 정렬한다.
            stack.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        private void RenameLids()
        {
            for (int i = 0; i < stack.Count; i++) stack[i].name = $"{LidNamePrefix}{i + 1:00}";
        }

        // 로봇암 B가 없는 지금은 인스펙터에서 직접 눌러 확인한다. (플레이 모드에서 컴포넌트 우클릭)
        // 가져간 뚜껑은 위로 들어 올렸다가 사라지게 해서, 가져간 뒤 아래에서 올라오는 모습을 볼 수 있게 한다.
        [ContextMenu("Take Top Lid (Test)")]
        private void TakeTopLidForTest()
        {
            if (TryTakeTopLid(out Transform lid)) StartCoroutine(LiftAndDiscard(lid));
        }

        private System.Collections.IEnumerator LiftAndDiscard(Transform lid)
        {
            Vector3 start = lid.position;
            Vector3 end = start + Vector3.up * testLiftHeight;
            for (float t = 0f; t < testLiftSeconds; t += Time.deltaTime)
            {
                lid.position = Vector3.Lerp(start, end, t / testLiftSeconds);
                yield return null;
            }

            Destroy(lid.gameObject);
        }
    }
}
