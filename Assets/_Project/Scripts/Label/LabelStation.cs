using System.Collections.Generic;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Label
{
    /// <summary>
    /// 라벨 설비. 뚜껑이 덮인(LidClosed) 키트의 박스가 이 설비 앞을 지나가면 뚜껑 윗면에 라벨(병원명, 로트 번호, 키트 코드)을 자동으로 붙이고,
    /// 표시판에도 같은 글을 띄운다. (FR-11) 팩킹에서 뚜껑이 덮인 박스만 라벨을 받고, 불량으로 밀려난 박스는 뚜껑이 없으므로 받지 않는다.
    /// 컨베이어는 모르고, 푸셔처럼 앞의 감지 영역을 물리로 훑어 박스를 찾는다. 박스가 어느 키트의 것인지는 IKitCarrier로 읽는다.
    /// 박스를 세우지 않고 지나가는 동안 붙인다. 한 키트에는 한 번만 붙인다.
    /// </summary>
    public sealed class LabelStation : MonoBehaviour
    {
        private const string DisplayTextHolderName = "DisplayText";
        private const string IdleDisplayText = "라벨 대기";

        // 표시판 글자를 판 표면보다 이만큼(m) 띄운다.
        private const float DisplayTextLiftM = 0.002f;

        // 에디터에서 선택했을 때 감지 영역을 그리는 색.
        private static readonly Color SpotGizmoColor = new Color(0.2f, 0.8f, 1f, 0.8f);

        [Header("박스 찾기")]
        [Tooltip("박스가 지나가는 자리의 중심, 이 오브젝트 기준 로컬 좌표(m). 컨베이어 중심선 위, 박스 높이쯤이다.")]
        [SerializeField] private Vector3 _spotLocal = new Vector3(0f, 0.92f, 0f);

        [Tooltip("감지 영역 절반 크기(m). 이 안에 박스가 들어오면 라벨을 붙인다.")]
        [SerializeField] private Vector3 _spotHalfExtents = new Vector3(0.15f, 0.15f, 0.15f);

        [Header("라벨")]
        [Tooltip("라벨 종이와 글자의 모양.")]
        [SerializeField] private KitLabelLook _look = new KitLabelLook();

        [Tooltip("라벨 종이 재질. 비워 두면 종이가 재질 없이 만들어진다.")]
        [SerializeField] private Material _paperMaterial;

        [Header("표시판")]
        [Tooltip("라벨 내용을 띄우는 표시판(LabelDisplay). 비워 두면 표시판 없이 라벨만 붙인다. 글자는 표시판 앞뒤 면에 띄운다.")]
        [SerializeField] private Transform _display;

        [Tooltip("표시판 글자 크기(TextMesh의 Character Size).")]
        [SerializeField, Min(0.0001f)] private float _displayCharacterSize = 0.01f;

        private readonly HashSet<KitData> _awaiting = new HashSet<KitData>();
        private readonly Collider[] _hits = new Collider[32];
        private KitLabelBuilder _builder;
        private TextMesh[] _displayTexts = new TextMesh[0];

        /// <summary>지금까지 라벨을 붙인 키트 수.</summary>
        public int LabeledCount { get; private set; }

        /// <summary>감지 영역의 중심(월드).</summary>
        public Vector3 SpotWorld => transform.TransformPoint(_spotLocal);

        private void Awake()
        {
            Font font = LabelFont.Resolve(_look.FontNames, _look.FontSize);
            _builder = new KitLabelBuilder(_look, _paperMaterial, font);
            BuildDisplayTexts();
        }

        private void OnEnable()
        {
            CellEvents.LidClosed += OnLidClosed;
        }

        private void OnDisable()
        {
            CellEvents.LidClosed -= OnLidClosed;
            _awaiting.Clear();
        }

        private void Update()
        {
            if (_awaiting.Count == 0)
            {
                return;
            }

            int count = Physics.OverlapBoxNonAlloc(SpotWorld, _spotHalfExtents, _hits, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                IKitCarrier carrier = _hits[i].GetComponentInParent<IKitCarrier>();
                if (carrier != null && carrier.Kit != null && _awaiting.Remove(carrier.Kit))
                {
                    Attach(carrier);
                }
            }
        }

        // 뚜껑이 덮인 키트만 라벨을 받는다. 박스가 이 설비 앞에 올 때까지 기억해 둔다.
        private void OnLidClosed(KitData kit)
        {
            _awaiting.Add(kit);
        }

        private void Attach(IKitCarrier carrier)
        {
            var carrierComponent = (Component)carrier;
            KitData kit = carrier.Kit;
            if (!LidSurface.TryFind(carrierComponent.transform, CellObjectNames.LidPrefix, out Transform lid, out Vector3 topCenter))
            {
                CellEvents.RaiseWarning(LabelWarningCodes.NoLid, $"키트 {kit.KitSequence}의 박스에서 뚜껑을 찾지 못해 라벨을 붙이지 못했다. (로트 {kit.LotNumber})");
                return;
            }

            string[] lines = LabelText.Compose(kit);
            _builder.BuildOnLid(kit, lines, lid, topCenter);
            ShowOnDisplay(lines);
            LabeledCount++;
        }

        // 표시판은 비균등 크기라 글자를 판의 자식으로 두면 글자가 찌그러진다. 크기 1인 자신의 자식 두 개(앞면, 뒷면)로 만든다.
        private void BuildDisplayTexts()
        {
            if (_display == null)
            {
                return;
            }

            var holder = new GameObject(DisplayTextHolderName).transform;
            holder.SetParent(transform, false);

            Vector3 center = transform.InverseTransformPoint(_display.position);
            float offset = _display.lossyScale.z * 0.5f + DisplayTextLiftM;
            Quaternion toward = Quaternion.identity;
            Quaternion away = Quaternion.Euler(0f, 180f, 0f);

            // TextMesh의 앞면은 -Z이므로, -Z 쪽 면에는 그대로, +Z 쪽 면에는 180도 돌려서 두 쪽에서 모두 바로 읽히게 한다.
            _displayTexts = new[]
            {
                _builder.CreateText(holder, center + Vector3.back * offset, toward, IdleDisplayText, _displayCharacterSize),
                _builder.CreateText(holder, center + Vector3.forward * offset, away, IdleDisplayText, _displayCharacterSize)
            };
        }

        private void ShowOnDisplay(string[] lines)
        {
            string text = LabelText.Join(lines);
            foreach (TextMesh mesh in _displayTexts)
            {
                mesh.text = text;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = SpotGizmoColor;
            Gizmos.matrix = Matrix4x4.TRS(SpotWorld, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, _spotHalfExtents * 2f);
        }
    }
}
