using System.Collections;
using System.Collections.Generic;
using ConveyorSystem;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 검사 구간 컨베이어(Conv_Inspection_01 ~ 06)를 실제로 움직이는 제어. (FR-05 일부)
    /// 박스를 01 입구에 올리면 컨베이어가 02 중심으로 옮기고, 거기서 멈춰 AtInspection을 보낸다. 판정(InspectionResult)이 나오면
    /// 정상 박스는 06 중심까지 보냈다가 지우고, 불량 박스는 04 중심에서 멈춰 푸셔가 가져가기를 기다린다. 푸셔가 박스를 풀면(IBoxConveyorRelease) 다음 박스를 받을 수 있다.
    /// 멈추는 방식: 박스가 칸 중심 가까이 오면 그 칸의 Speed를 0으로 하고 박스를 중심에 맞춘 뒤, 풀어 줄 때 Speed를 되돌린다.
    /// 한 번에 박스 하나만 시험한다. 키팅과 나머지 컨베이어를 구현하면 이 컴포넌트는 그 구현으로 합친다.
    /// </summary>
    public sealed class InspectionConveyorLine : MonoBehaviour
    {
        [Header("컨베이어 칸")]
        [Tooltip("컨베이어 오브젝트 이름의 앞부분. 뒤에 두 자리 번호(01, 02, ...)가 붙는다.")]
        [SerializeField] private string _namePrefix = "Conv_Inspection_";
        [Tooltip("박스를 올리는 칸 번호.")]
        [SerializeField, Min(1)] private int _entryIndex = 1;
        [Tooltip("검사 자리 칸 번호. 여기서 멈춰 AtInspection을 보낸다.")]
        [SerializeField, Min(1)] private int _inspectIndex = 2;
        [Tooltip("푸셔 자리 칸 번호. 불량 박스가 여기서 멈춘다.")]
        [SerializeField, Min(1)] private int _pushIndex = 4;
        [Tooltip("끝 칸 번호(패킹 자리). 정상 박스가 여기서 멈췄다가 지워진다.")]
        [SerializeField, Min(1)] private int _endIndex = 6;

        [Header("시간과 거리")]
        [Tooltip("박스가 칸 중심에서 이 거리 안에 들어오면 멈추고 중심에 맞춘다(m). 한 프레임에 움직이는 거리(속도 2m/s에서 약 3cm)보다 커야 한다.")]
        [SerializeField, Min(0.05f)] private float _stopRadius = 0.08f;
        [Tooltip("검사 결과를 기다리는 최대 시간(초). 넘으면 경고를 남기고 박스를 그대로 보낸다. 시뮬레이션 시간이다.")]
        [SerializeField, Min(1f)] private float _resultWaitSeconds = 10f;
        [Tooltip("불량 박스를 푸셔가 가져가기를 기다리는 최대 시간(초). 넘으면 경고를 남기고 계속 기다린다. 시뮬레이션 시간이다.")]
        [SerializeField, Min(1f)] private float _pushWaitSeconds = 30f;
        [Tooltip("정상 박스가 끝 칸 중심에서 멈춰 있다가 지워지기까지의 시간(초). 시뮬레이션 시간이다.")]
        [SerializeField, Min(0f)] private float _endHoldSeconds = 3f;

        private readonly Dictionary<ConveyorBase, float> _savedSpeed = new Dictionary<ConveyorBase, float>();
        private readonly HashSet<KitData> _resultKits = new HashSet<KitData>();
        private ConveyorBase _entry;
        private ConveyorBase _inspect;
        private ConveyorBase _push;
        private ConveyorBase _end;

        /// <summary>컨베이어 칸을 모두 찾아서 박스를 받을 수 있으면 true.</summary>
        public bool IsReady => _entry != null && _inspect != null && _push != null && _end != null;

        private void Awake()
        {
            _entry = Find(_entryIndex);
            _inspect = Find(_inspectIndex);
            _push = Find(_pushIndex);
            _end = Find(_endIndex);

            if (!IsReady)
            {
                Debug.LogError($"{name}: {_namePrefix}{_entryIndex:00}, {_inspectIndex:00}, {_pushIndex:00}, {_endIndex:00} 컨베이어를 모두 찾아야 한다.", this);
            }
        }

        private void OnEnable()
        {
            CellEvents.InspectionResult += OnInspectionResult;
            Subscribe(_inspect, true);
            Subscribe(_push, true);
            Subscribe(_end, true);
        }

        private void OnDisable()
        {
            CellEvents.InspectionResult -= OnInspectionResult;
            Subscribe(_inspect, false);
            Subscribe(_push, false);
            Subscribe(_end, false);
        }

        /// <summary>
        /// 박스를 01 입구에 올린다. 박스에 Parcel과 KitBoxParcel을 붙이고 컨베이어에 넘긴다. 올리지 못하면 false이다(01에 이미 박스가 있을 때 등).
        /// AtInspection은 박스가 02 중심에 도착하면 이 컴포넌트가 보낸다.
        /// </summary>
        public bool TryPlaceBox(GameObject box, KitData kit)
        {
            if (!IsReady || box == null || kit == null)
            {
                return false;
            }

            var carrier = box.GetComponent<KitBoxParcel>();
            if (carrier == null)
            {
                carrier = box.AddComponent<KitBoxParcel>();
            }

            carrier.Configure(kit);
            return carrier.Parcel.PlaceOnConveyor(_entry);
        }

        private ConveyorBase Find(int index)
        {
            string wanted = $"{_namePrefix}{index:00}";
            foreach (ConveyorBase conveyor in FindObjectsByType<ConveyorBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (conveyor.name == wanted)
                {
                    return conveyor;
                }
            }

            return null;
        }

        private void Subscribe(ConveyorBase conveyor, bool on)
        {
            if (conveyor == null)
            {
                return;
            }

            if (on)
            {
                conveyor.ParcelEntered += OnParcelEntered;
                conveyor.ParcelExited += OnParcelExited;
            }
            else
            {
                conveyor.ParcelEntered -= OnParcelEntered;
                conveyor.ParcelExited -= OnParcelExited;
            }
        }

        private void OnInspectionResult(KitData kit)
        {
            _resultKits.Add(kit);
        }

        private void OnParcelEntered(ConveyorBase conveyor, Parcel parcel)
        {
            var carrier = parcel.GetComponent<KitBoxParcel>();
            if (carrier == null || carrier.Kit == null)
            {
                return;
            }

            if (conveyor == _inspect)
            {
                StartCoroutine(AtInspectionRoutine(conveyor, carrier));
            }
            else if (conveyor == _push)
            {
                StartCoroutine(AtPushRoutine(conveyor, carrier));
            }
            else if (conveyor == _end)
            {
                StartCoroutine(AtEndRoutine(conveyor, carrier));
            }
        }

        // 박스가 칸을 떠나면(다음 칸으로 넘어가거나 외부에서 풀림) 멈춰 둔 속도를 되돌린다.
        private void OnParcelExited(ConveyorBase conveyor, Parcel parcel)
        {
            Resume(conveyor);
        }

        private IEnumerator AtInspectionRoutine(ConveyorBase conveyor, KitBoxParcel carrier)
        {
            yield return StopAtCenter(conveyor, carrier.Parcel);
            if (carrier == null || carrier.Parcel.CurrentConveyor != conveyor)
            {
                yield break;
            }

            KitData kit = carrier.Kit;
            CellEvents.RaiseAtInspection(kit);

            float deadline = Time.time + _resultWaitSeconds;
            while (!_resultKits.Contains(kit) && Time.time < deadline && carrier != null)
            {
                yield return null;
            }

            if (carrier == null)
            {
                yield break;
            }

            if (!_resultKits.Contains(kit))
            {
                CellEvents.RaiseWarning("CONVEYOR_RESULT_TIMEOUT", $"검사 결과가 {_resultWaitSeconds:F0}초 안에 오지 않아 박스를 그대로 보낸다. (로트 {kit.LotNumber})");
            }

            Resume(conveyor);
        }

        // 불량 박스만 멈춘다. 정상 박스는 04를 그냥 지나간다. 푸셔가 박스를 풀면 ParcelExited로 속도가 돌아온다.
        private IEnumerator AtPushRoutine(ConveyorBase conveyor, KitBoxParcel carrier)
        {
            KitData kit = carrier.Kit;
            if (!kit.Verdict.HasValue || kit.Verdict.Value.IsPass)
            {
                yield break;
            }

            yield return StopAtCenter(conveyor, carrier.Parcel);

            float deadline = Time.time + _pushWaitSeconds;
            while (carrier != null && carrier.Parcel.CurrentConveyor == conveyor)
            {
                if (Time.time >= deadline)
                {
                    CellEvents.RaiseWarning("CONVEYOR_PUSH_WAIT", $"불량 박스를 푸셔가 {_pushWaitSeconds:F0}초 안에 가져가지 않았다. 계속 기다린다. (로트 {kit.LotNumber})");
                    deadline = Time.time + _pushWaitSeconds;
                }

                yield return null;
            }
        }

        // 정상 박스는 끝 칸 중심에서 멈췄다가 지워진다. (패킹은 이 구간 밖이다)
        private IEnumerator AtEndRoutine(ConveyorBase conveyor, KitBoxParcel carrier)
        {
            yield return StopAtCenter(conveyor, carrier.Parcel);
            if (carrier == null || carrier.Parcel.CurrentConveyor != conveyor)
            {
                yield break;
            }

            if (_endHoldSeconds > 0f)
            {
                yield return new WaitForSeconds(_endHoldSeconds);
            }

            if (carrier == null)
            {
                yield break;
            }

            GameObject box = carrier.gameObject;
            conveyor.RemoveParcel(carrier.Parcel);
            Destroy(box);
        }

        // 박스가 칸 중심 가까이 올 때까지 기다렸다가 칸을 멈추고 박스를 중심에 맞춘다.
        private IEnumerator StopAtCenter(ConveyorBase conveyor, Parcel parcel)
        {
            Transform t = parcel.transform;
            while (parcel != null && parcel.CurrentConveyor == conveyor
                   && Vector3.Distance(t.position, conveyor.CenterPoint.position) > _stopRadius)
            {
                yield return null;
            }

            if (parcel == null || parcel.CurrentConveyor != conveyor)
            {
                yield break;
            }

            if (!_savedSpeed.ContainsKey(conveyor))
            {
                _savedSpeed[conveyor] = conveyor.Speed;
            }

            conveyor.Speed = 0f;
            t.position = conveyor.CenterPoint.position;
        }

        private void Resume(ConveyorBase conveyor)
        {
            if (conveyor != null && _savedSpeed.TryGetValue(conveyor, out float speed))
            {
                conveyor.Speed = speed;
                _savedSpeed.Remove(conveyor);
            }
        }

        private void OnDestroy()
        {
            // 플레이를 끝낼 때 멈춰 둔 칸이 남아 있어도 에디터의 값이 바뀌지 않도록 되돌린다.
            foreach (KeyValuePair<ConveyorBase, float> pair in _savedSpeed)
            {
                if (pair.Key != null)
                {
                    pair.Key.Speed = pair.Value;
                }
            }
        }
    }
}
