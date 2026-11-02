using System.Collections;
using System.Collections.Generic;
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

        [Tooltip("이 거리 안에서 가장 가까운 박스를 검사 대상으로 본다(m).")]
        [SerializeField, Min(0.05f)] private float _boxSearchRadius = 0.4f;

        [Tooltip("판정에 걸리는 시간(초). 시뮬레이션 시간이다.")]
        [SerializeField, Min(0f)] private float _inspectSeconds = 0.5f;

        private readonly Dictionary<string, RecipeSO> _recipeByKit = new Dictionary<string, RecipeSO>();
        private IStationStateReporter _reporter;
        private KitInspection _inspection;
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

            kit.Verdict = _inspection.Run(kit, FindBoxSensors());
            CellEvents.RaiseInspectionResult(kit);

            _reporter.Set(StationState.Idle);
            _busy = false;
        }

        // 구역 가까이에 있는 박스의 센서 묶음. 없으면 null이며, 규칙 판정에서는 센서 무응답으로 본다.
        private SlotSensorArray FindBoxSensors()
        {
            SlotSensorArray nearest = null;
            float nearestSqr = _boxSearchRadius * _boxSearchRadius;

            foreach (SlotSensorArray array in FindObjectsByType<SlotSensorArray>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                float sqr = (array.transform.position - transform.position).sqrMagnitude;
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
