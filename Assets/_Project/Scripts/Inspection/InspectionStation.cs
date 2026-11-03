using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MediKitTwin.Common;
using MediKitTwin.Recipes;
using UnityEngine;

namespace MediKitTwin.Inspection
{
    /// <summary>
    /// 검사 구역. AtInspection을 받으면 구역 안의 박스(SlotSensorArray)를 판정하고, 결과를 KitData.Verdict에 채운 뒤
    /// InspectionResult를 보낸다. (FR-06, FR-07) 판정 순서는 KitInspection이, 슬롯 읽기는 SlotSensorArray가 맡는다.
    /// 시간은 시뮬레이션 시간이라 배속과 일시정지를 따른다.
    /// </summary>
    public sealed class InspectionStation : MonoBehaviour
    {
        [Tooltip("키트 코드로 정답 구성을 찾는 레시피 목록.")]
        [SerializeField] private List<RecipeSO> _recipes = new List<RecipeSO>();

        [Tooltip("주 판정기(IInspector, 보통 CNN). 비워 두면 규칙 판정만 쓴다. 판정할 수 없으면 규칙 판정으로 대체한다.")]
        [SerializeField] private MonoBehaviour _primaryInspector;

        [Tooltip("바닥면(수평) 거리가 이 안에서 가장 가까운 박스를 검사 대상으로 본다(m). 높이는 따지지 않는다.")]
        [SerializeField, Min(0.05f)] private float _boxSearchRadius = 0.4f;

        [Tooltip("판정에 걸리는 시간(초). 시뮬레이션 시간이다.")]
        [SerializeField, Min(0f)] private float _inspectSeconds = 0.5f;

        [Tooltip("검사 카메라. 비워 두면 이 오브젝트나 자식의 InspectionCameraRig를 쓰고, 그것도 없으면 촬영하지 않는다.")]
        [SerializeField] private InspectionCameraRig _cameraRig;

        [Tooltip("촬영한 전체 이미지와 슬롯 크롭을 프로젝트 폴더의 InspectionCaptures/<플레이 시작 시각>/<로트번호>에 PNG로 저장한다. 플레이할 때마다 새 폴더가 생겨 이전 결과를 덮어쓰지 않는다. 크롭이 맞게 잘리는지 눈으로 확인할 때 쓴다.")]
        [SerializeField] private bool _saveCaptures;

        private readonly Dictionary<string, RecipeSO> _recipeByKit = new Dictionary<string, RecipeSO>();
        private IStationStateReporter _reporter;
        private KitInspection _inspection;
        private readonly string _captureSession = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        private StationCropSource _cropSource;
        private bool _busy;

        private void Awake()
        {
            foreach (RecipeSO recipe in _recipes)
            {
                if (recipe != null)
                {
                    _recipeByKit[recipe.KitCode] = recipe;
                }
            }

            IInspector primary = null;
            if (_primaryInspector != null)
            {
                primary = _primaryInspector as IInspector;
                if (primary == null)
                {
                    Debug.LogError($"{name}: Primary Inspector는 IInspector를 구현해야 한다.", this);
                }
            }

            if (_cameraRig == null)
            {
                _cameraRig = GetComponentInChildren<InspectionCameraRig>();
            }

            _cropSource = new StationCropSource(() => _cameraRig);
            (_primaryInspector as IStationBoundInspector)?.Bind(FindRecipe, _cropSource);

            _reporter = new StationStateReporter(StationId.Inspection);
            _inspection = new KitInspection(FindRecipe, primary, CellEvents.RaiseWarning);
        }

        private void OnEnable()
        {
            CellEvents.AtInspection += OnAtInspection;
        }

        private void OnDisable()
        {
            CellEvents.AtInspection -= OnAtInspection;
        }

        private void Update()
        {
            _reporter.Tick(Time.unscaledTime);
        }

        private RecipeSO FindRecipe(string kitCode)
        {
            return kitCode != null && _recipeByKit.TryGetValue(kitCode, out RecipeSO recipe) ? recipe : null;
        }

        private void OnAtInspection(KitData kit)
        {
            if (_busy)
            {
                CellEvents.RaiseWarning("INSPECTION_BUSY", $"검사 중에 다음 키트가 도착했다. 무시한다. (로트 {kit.LotNumber})");
                return;
            }

            StartCoroutine(Inspect(kit));
        }

        private IEnumerator Inspect(KitData kit)
        {
            _busy = true;
            _reporter.Set(StationState.Running);

            yield return new WaitForSeconds(_inspectSeconds);

            SlotSensorArray box = FindBoxSensors();
            if (_saveCaptures)
            {
                SaveCaptures(kit, box);
            }

            _cropSource.Box = box != null ? box.transform : null;
            kit.Verdict = _inspection.Run(kit, box);
            _cropSource.Box = null;
            CellEvents.RaiseInspectionResult(kit);

            _reporter.Set(StationState.Idle);
            _busy = false;
        }

        // 박스를 찍어 슬롯 크롭과 함께 PNG로 저장한다. 판정에는 쓰지 않는다. (크롭 확인용, CNN 판정은 이 크롭을 입력으로 쓸 예정)
        private void SaveCaptures(KitData kit, SlotSensorArray box)
        {
            if (_cameraRig == null || box == null || !_recipeByKit.TryGetValue(kit.KitCode ?? string.Empty, out RecipeSO recipe))
            {
                CellEvents.RaiseWarning("INSPECTION_CAPTURE_SKIPPED", $"촬영할 카메라, 박스, 레시피 중 없는 것이 있어 건너뛴다. (로트 {kit.LotNumber})");
                return;
            }

            if (!_cameraRig.TryCaptureCrops(box.transform, recipe, out SlotCropSet crops, out Texture2D full))
            {
                if (full != null)
                {
                    Destroy(full);
                }

                CellEvents.RaiseWarning("INSPECTION_CROP_FAILED", $"슬롯 크롭을 만들지 못했다. 검사 카메라 설정을 확인한다. (로트 {kit.LotNumber})");
                return;
            }

            string sessionFolder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "InspectionCaptures", _captureSession);
            string folder = Path.Combine(sessionFolder, kit.LotNumber);
            for (int copy = 2; Directory.Exists(folder); copy++)
            {
                folder = Path.Combine(sessionFolder, $"{kit.LotNumber}_{copy}");
            }

            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "full.png"), full.EncodeToPNG());
            foreach (SlotCrop crop in crops.Crops)
            {
                File.WriteAllBytes(Path.Combine(folder, $"slot{crop.SlotNumber}.png"), crop.Image.EncodeToPNG());
            }

            Debug.Log($"[Inspection] 촬영 이미지를 저장했다: {folder}", this);
            crops.Dispose();
            Destroy(full);
        }

        // 주 판정기(CNN)에 지금 검사 중인 박스의 슬롯 크롭을 건넨다. 박스나 카메라가 없으면 크롭을 만들 수 없다(false).
        private sealed class StationCropSource : ISlotCropSource
        {
            private readonly Func<InspectionCameraRig> _rig;

            public StationCropSource(Func<InspectionCameraRig> rig)
            {
                _rig = rig;
            }

            public Transform Box { get; set; }

            public bool TryGetCrops(KitData kit, RecipeSO recipe, out SlotCropSet crops)
            {
                crops = null;
                InspectionCameraRig rig = _rig();
                if (rig == null || Box == null)
                {
                    return false;
                }

                bool ok = rig.TryCaptureCrops(Box, recipe, out crops, out Texture2D full);
                if (full != null)
                {
                    UnityEngine.Object.Destroy(full);
                }

                return ok;
            }
        }

        // 구역 가까이에 있는 박스의 센서 묶음. 없으면 null이며, 규칙 판정에서는 센서 무응답으로 본다.
        private SlotSensorArray FindBoxSensors()
        {
            SlotSensorArray nearest = null;
            float nearestSqr = _boxSearchRadius * _boxSearchRadius;

            foreach (SlotSensorArray array in FindObjectsByType<SlotSensorArray>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Vector3 offset = array.transform.position - transform.position;
                offset.y = 0f;
                float sqr = offset.sqrMagnitude;
                if (sqr <= nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = array;
                }
            }

            return nearest;
        }
    }
}
