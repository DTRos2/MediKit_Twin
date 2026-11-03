using System;
using System.Collections;
using System.Collections.Generic;
using MediKitTwin.ArmA;
using MediKitTwin.Common;
using MediKitTwin.Conveyor;
using MediKitTwin.Inspection;
using MediKitTwin.Inventory;
using MediKitTwin.Recipes;
using UnityEngine;
using Random = System.Random;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 키팅이 구현되기 전까지 검사 구역을 확인하는 임시 장치다. 키팅이 끝난 것처럼 레시피대로 채운 샘플 박스를
    /// Sensor_Inspection 자리에 만들고 AtInspection을 보낸다. 정상, 누락, 오구성, 센서 무응답 박스를 골라 만들 수 있다.
    /// 플레이 모드에서 컴포넌트를 우클릭해 'Spawn ...' 항목을 누른다. 키팅과 컨베이어를 구현하면 이 컴포넌트는 지운다.
    /// 박스는 지그의 KitBox를 복제하므로 슬롯 센서가 그대로 따라온다. 구성품은 합성 데이터 생성기와 같은 모양, 색, 크기로 만든다.
    /// </summary>
    public sealed class InspectionSampleSpawner : MonoBehaviour
    {
        private const string BoxName = "KitBox";
        private const string SpawnPointName = "Sensor_Inspection";

        [Header("재료")]
        [Tooltip("정답 구성을 담은 레시피(EM, BL, DX).")]
        [SerializeField] private List<RecipeSO> _recipes = new List<RecipeSO>();
        [Tooltip("구성품의 모양과 색.")]
        [SerializeField] private ShelfItemLooksSO _looks;
        [Tooltip("구성품이 그리퍼에 맞게 줄어드는 비율을 정하는 선반 설정.")]
        [SerializeField] private ShelfLayoutConfigSO _shelfLayout;

        [Header("만들 샘플")]
        [Tooltip("만들 키트의 종류(EM, BL, DX).")]
        [SerializeField] private string _kitCode = "EM";
        [Tooltip("누락, 오구성, 센서 무응답을 만들 슬롯 번호. 0이면 무작위로 고른다.")]
        [SerializeField, Min(0)] private int _targetSlot;
        [Tooltip("같은 값이면 같은 무작위 결과가 나온다. (NFR-06)")]
        [SerializeField] private int _seed = 1;

        [Header("놓는 자리")]
        [Tooltip("박스를 놓을 기준. 비워 두면 자식 Sensor_Inspection, 그것도 없으면 이 오브젝트를 쓴다.")]
        [SerializeField] private Transform _spawnPoint;
        [Tooltip("기준점에서 박스 중심까지의 높이 차이(m). 컨베이어 위에 박스 바닥이 닿도록 맞춘다.")]
        [SerializeField] private float _boxCenterOffsetY = -0.10f;
        [Tooltip("박스를 놓는 방향(도). 슬롯이 박스의 Z축을 따라 놓여 있다.")]
        [SerializeField] private float _boxYawDegrees;
        [Tooltip("박스 중심에서 안쪽 바닥면까지의 로컬 높이(m). 박스 높이의 절반에서 벽 두께를 뺀 값이다.")]
        [SerializeField] private float _boxFloorLocalY = -0.04f;

        [Header("컨베이어")]
        [Tooltip("씬에 InspectionConveyorLine이 있으면 박스를 Conv_Inspection_01에 올려 컨베이어로 검사 자리와 푸셔 자리까지 옮긴다. 끄거나 없으면 박스를 검사 자리에 바로 놓는다.")]
        [SerializeField] private bool _useConveyor = true;

        [Header("불량 박스 이동")]
        [Tooltip("(컨베이어를 쓰지 않을 때만) 불량으로 판정된 박스를 푸셔 앞으로 옮긴다. 컨베이어가 없어서 박스가 푸셔까지 오지 않기 때문이며, 컨베이어를 구현하면 끈다.")]
        [SerializeField] private bool _sendRejectedToPusher = true;
        [Tooltip("판정 뒤 박스를 푸셔 앞으로 옮기기까지의 시간(초). 컨베이어로 이동하는 시간을 흉내 낸다. 시뮬레이션 시간이다.")]
        [SerializeField, Min(0f)] private float _travelSeconds = 1f;

        [Header("위치 흔들림 (실제 키팅 편차 흉내)")]
        [Tooltip("학습 랜덤화 범위에 곱하는 배율. 0이면 흔들림 없음, 1이면 학습 범위(박스 ±1cm/±5도, 구성품 ±6mm/±20도) 끝까지, 2면 두 배.")]
        [SerializeField, Min(0f)] private float _jitterScale;
        [SerializeField, Min(0f)] private float _boxOffsetMeters = 0.01f;
        [SerializeField, Min(0f)] private float _boxYawJitterDegrees = 5f;
        [SerializeField, Min(0f)] private float _itemOffsetMeters = 0.006f;
        [SerializeField, Min(0f)] private float _itemYawJitterDegrees = 20f;

        private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
        private GameObject _template;
        private GameObject _sample;
        private GraspSizeLimit _graspLimit;
        private KitData _currentKit;
        private string _currentNote;
        private int _count;
        private bool _onConveyor;

        private void Awake()
        {
            KitBoxSupply supply = FindFirstObjectByType<KitBoxSupply>();
            Transform source = supply != null ? supply.transform.Find(BoxName) : null;
            if (source == null || _looks == null || _shelfLayout == null)
            {
                Debug.LogError($"{name}: 지그의 KitBox와 Looks, Shelf Layout이 있어야 한다.", this);
                enabled = false;
                return;
            }

            // 지그가 박스를 치우거나 바꿔도 되도록 처음 모양을 복제해 둔다.
            _template = Instantiate(source.gameObject, transform);
            _template.name = BoxName + "SampleTemplate";
            _template.SetActive(false);

            if (_spawnPoint == null)
            {
                _spawnPoint = transform.Find(SpawnPointName) ?? transform;
            }

            _graspLimit = new GraspSizeLimit(_shelfLayout.GripperOpeningWidthM, _shelfLayout.GraspClearanceRatio);
        }

        private void OnEnable()
        {
            CellEvents.InspectionResult += OnInspectionResult;
        }

        private void OnDisable()
        {
            CellEvents.InspectionResult -= OnInspectionResult;
        }

        private void OnDestroy()
        {
            foreach (Material material in _materials.Values)
            {
                Destroy(material);
            }
        }

        [ContextMenu("Spawn Normal Box And Inspect")]
        private void SpawnNormal() => Spawn(SampleVariant.Normal);

        [ContextMenu("Spawn Missing Box And Inspect")]
        private void SpawnMissing() => Spawn(SampleVariant.Missing);

        [ContextMenu("Spawn Wrong Item Box And Inspect")]
        private void SpawnWrongItem() => Spawn(SampleVariant.WrongItem);

        [ContextMenu("Spawn Sensor Failure Box And Inspect")]
        private void SpawnSensorFailure() => Spawn(SampleVariant.SensorFailure);

        /// <summary>false이면 결과 로그를 남기지 않는다. 자동 측정(InspectionBenchmark)이 대량으로 돌릴 때 끈다.</summary>
        public bool LogResults { get; set; } = true;

        /// <summary>false이면 박스를 컨베이어에 올리지 않고 검사 자리에 바로 놓는다. 자동 측정(InspectionBenchmark)이 대량으로 돌릴 때 끈다.</summary>
        public bool UseConveyor { get => _useConveyor; set => _useConveyor = value; }

        /// <summary>false이면 불량 박스를 푸셔 앞으로 옮기지 않는다. 자동 측정(InspectionBenchmark)이 대량으로 돌릴 때 끈다.</summary>
        public bool SendRejectedToPusher { get => _sendRejectedToPusher; set => _sendRejectedToPusher = value; }

        /// <summary>위치 흔들림 배율. 자동 측정이 단계별로 바꿔 가며 쓴다.</summary>
        public float JitterScale { get => _jitterScale; set => _jitterScale = Mathf.Max(0f, value); }

        private void Spawn(SampleVariant variant) => TrySpawn(variant, _kitCode, _targetSlot, out _, out _);

        /// <summary>
        /// 샘플 박스를 만들고 AtInspection을 보낸다. targetSlot이 0이면 무작위로 고르며, 실제 고른 슬롯을 target으로 돌려준다.
        /// 정상 박스에서도 target은 값이 있지만 쓰이지 않는다.
        /// </summary>
        public bool TrySpawn(SampleVariant variant, string kitCode, int targetSlot, out KitData kit, out int target)
        {
            kit = null;
            target = 0;
            if (!Application.isPlaying || _template == null)
            {
                Debug.LogWarning($"{name}: 플레이 모드에서만 샘플 박스를 만들 수 있다.", this);
                return false;
            }

            RecipeSO recipe = _recipes.Find(r => r != null && r.KitCode == kitCode.Trim().ToUpperInvariant());
            if (recipe == null)
            {
                Debug.LogError($"{name}: 키트 '{kitCode}'의 레시피가 Recipes에 없다.", this);
                return false;
            }

            _count++;
            var rng = new Random(unchecked(_seed * 7919 + _count));
            int slotCount = recipe.Items.Count;
            target = targetSlot >= 1 && targetSlot <= slotCount ? targetSlot : rng.Next(1, slotCount + 1);
            string[] contents = SampleBoxPlan.Plan(recipe, variant, target, AllComponentIds(), rng);

            if (_sample != null)
            {
                // 컨베이어에 실려 있으면 먼저 풀어야 컨베이어가 지워진 박스를 계속 옮기려 하지 않는다.
                _sample.GetComponent<IBoxConveyorRelease>()?.ReleaseToPhysics();
                Destroy(_sample);
            }

            _sample = BuildBox(recipe, contents, rng);
            if (variant == SampleVariant.SensorFailure)
            {
                foreach (SlotSensor sensor in _sample.GetComponentsInChildren<SlotSensor>())
                {
                    if (sensor.SlotNumber == target)
                    {
                        sensor.IsResponsive = false;
                    }
                }
            }

            _currentKit = new KitData(_count, _count, $"SAMPLE-{_count:0000}", "샘플병원", recipe.KitCode);
            if (variant == SampleVariant.Missing)
            {
                _currentKit.SetMissingAnswer(target);
            }

            _currentNote = variant == SampleVariant.Normal ? "정상 박스" : $"{variant} 박스, 슬롯 {target}";

            kit = _currentKit;

            // 컨베이어가 있으면 박스를 01 입구에 올린다. AtInspection은 박스가 검사 자리에 도착했을 때 컨베이어 쪽이 보낸다.
            InspectionConveyorLine line = _useConveyor ? FindFirstObjectByType<InspectionConveyorLine>() : null;
            if (line != null && line.TryPlaceBox(_sample, _currentKit))
            {
                _onConveyor = true;
                return true;
            }

            if (line != null)
            {
                Debug.LogWarning($"{name}: 컨베이어에 박스를 올리지 못했다(01에 박스가 있거나 칸을 못 찾음). 검사 자리에 바로 놓는다.", this);
            }

            // 박스가 놓인 뒤 한 프레임 기다리지 않아도 센서는 위치로 읽으므로 바로 알린다.
            _onConveyor = false;
            CellEvents.RaiseAtInspection(_currentKit);
            return true;
        }

        private GameObject BuildBox(RecipeSO recipe, string[] contents, Random rng)
        {
            Vector3 position = _spawnPoint.position + Vector3.up * _boxCenterOffsetY
                + new Vector3(Jitter(rng, _boxOffsetMeters), 0f, Jitter(rng, _boxOffsetMeters));
            GameObject box = Instantiate(_template, position, Quaternion.Euler(0f, _boxYawDegrees + Jitter(rng, _boxYawJitterDegrees), 0f), transform);
            box.name = BoxName + "Sample";
            box.SetActive(true);

            foreach (RecipeItem item in recipe.Items)
            {
                string componentId = contents[item.SlotNumber - 1];
                if (componentId != null)
                {
                    AddItem(box.transform, componentId, item.SlotLocalPosition, rng);
                }
            }

            return box;
        }

        private void AddItem(Transform box, string componentId, Vector3 slotLocal, Random rng)
        {
            ShelfItemLook look = _looks.GetLook(componentId, out _);
            float scale = _graspLimit.ScaleFor(look.Size.x, look.Size.y, look.Size.z);
            Vector3 size = look.Size * scale;

            PrimitiveType primitive = look.Shape == ShelfItemShape.Cylinder ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            GameObject item = GameObject.CreatePrimitive(primitive);
            item.name = $"Item_{componentId}";
            Destroy(item.GetComponent<Collider>());

            item.transform.SetParent(box, false);
            item.transform.localPosition = new Vector3(
                slotLocal.x + Jitter(rng, _itemOffsetMeters), _boxFloorLocalY + size.y * 0.5f, slotLocal.z + Jitter(rng, _itemOffsetMeters));
            item.transform.localRotation = Quaternion.Euler(0f, Jitter(rng, _itemYawJitterDegrees), 0f);

            // 원기둥 프리미티브는 높이가 scale.y의 2배이다.
            item.transform.localScale = primitive == PrimitiveType.Cylinder ? new Vector3(size.x, size.y * 0.5f, size.z) : size;
            item.GetComponent<Renderer>().sharedMaterial = MaterialFor(componentId, look.Color);
            item.AddComponent<KitItemLabel>().Configure(componentId);
        }

        // -range*scale ~ +range*scale 균등 분포. 배율 0이면 난수를 쓰지 않아 기존 박스와 같다.
        private float Jitter(Random rng, float range)
        {
            return _jitterScale <= 0f ? 0f : (float)(rng.NextDouble() * 2.0 - 1.0) * range * _jitterScale;
        }

        private Material MaterialFor(string componentId, Color color)
        {
            if (_materials.TryGetValue(componentId, out Material cached))
            {
                return cached;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            _materials[componentId] = material;
            return material;
        }

        private List<string> AllComponentIds()
        {
            var ids = new List<string>();
            foreach (RecipeSO recipe in _recipes)
            {
                if (recipe == null)
                {
                    continue;
                }

                foreach (RecipeItem item in recipe.Items)
                {
                    ids.Add(item.ComponentId);
                }
            }

            return ids;
        }

        private void OnInspectionResult(KitData kit)
        {
            if (kit != _currentKit)
            {
                return;
            }

            if (!_onConveyor && _sendRejectedToPusher && kit.Verdict.HasValue && !kit.Verdict.Value.IsPass)
            {
                StartCoroutine(SendToPusher(kit));
            }

            if (!LogResults)
            {
                return;
            }

            InspectionVerdict v = kit.Verdict.Value;
            string slots = v.MissingSlots == null ? "알 수 없음" : string.Join(",", v.MissingSlots);
            string detected = kit.DetectedComponentIds == null
                ? "없음"
                : string.Join(",", Array.ConvertAll(kit.DetectedComponentIds, id => id ?? "-"));
            Debug.Log($"[InspectionSample] {kit.LotNumber} ({_currentNote}) → {(v.IsPass ? "정상" : "불량")} / 불일치 슬롯: {slots} / 센서: {detected} / 방식: {v.Method}", this);
        }

        // 컨베이어가 없어서 불량 박스를 푸셔 앞 자리로 옮긴다. 박스의 흔들림(놓인 위치 편차)은 그대로 유지한다.
        private IEnumerator SendToPusher(KitData kit)
        {
            // 네임스페이스와 같은 이름이라 전체 이름으로 쓴다.
            var pusher = FindFirstObjectByType<MediKitTwin.Pusher.Pusher>();
            if (pusher == null)
            {
                Debug.LogWarning($"{name}: 씬에 Pusher가 없어 불량 박스를 옮기지 못한다.", this);
                yield break;
            }

            GameObject box = _sample;
            if (_travelSeconds > 0f)
            {
                yield return new WaitForSeconds(_travelSeconds);
            }

            // 기다리는 동안 새 샘플이 만들어졌으면 이 박스는 이미 지워졌다.
            if (box == null || kit != _currentKit)
            {
                yield break;
            }

            Vector3 jitter = box.transform.position - (_spawnPoint.position + Vector3.up * _boxCenterOffsetY);
            box.transform.position = pusher.BoxSpotWorld + new Vector3(jitter.x, 0f, jitter.z);
            Debug.Log($"[InspectionSample] 불량 박스({kit.LotNumber})를 푸셔 앞으로 옮겼다.", this);
        }
    }
}
