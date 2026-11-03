using System.Collections;
using ConveyorSystem;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 박스를 팩킹 구역의 정해진 자리에 세워 두고 팔 B가 뚜껑을 덮을 때까지 기다리게 한다. (FR-05, FR-10)
    /// 박스가 멈출 자리(Stop Point)를 지나가는 프레임에 구간을 멈추고(PauserConveyor) 진행 방향을 자리에 맞춘 뒤 옆으로 미끄러뜨려 자리에 세우고 AtPacking을 알리고,
    /// LidClosed가 오면 다시 출발시킨다. 구간이 멈춰 있는 동안에는 새 박스를 받지 않으므로 뒤따르는 박스는 앞 구간에서 기다린다.
    /// 팔 B는 이 구역 센서(Sensor_Packing)의 자손에서 KitBox를 찾으므로, 선 박스는 센서의 자식으로 옮겨 두었다가 출발할 때 돌려놓는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PackingStop : MonoBehaviour
    {
        [Tooltip("팩킹 구역에 놓인 컨베이어 구간. 멈출 자리가 이 구간의 입력점과 출력점 사이에 있어야 한다.")]
        [SerializeField] private PauserConveyor _conveyor;

        [Tooltip("팔 B가 덮을 박스를 찾는 부모(Sensor_Packing). 박스가 서 있는 동안 받침이 이 아래로 옮겨진다.")]
        [SerializeField] private Transform _packingSensor;

        [Tooltip("박스가 멈출 자리. 팔 B가 맞춰진 자리이다. 가로 위치만 쓰고 높이는 벨트가 정한다. 비워 두면 구간 가운데에서 멈춘다.")]
        [SerializeField] private Transform _stopPoint;

        [Tooltip("박스가 벨트 중심선에서 멈출 자리로 옆으로 미끄러지는 속도(m/s). 팔 B가 닿는 자리가 벨트 한쪽에 치우쳐 있어서 중심선 위로는 닿지 못한다.")]
        [SerializeField, Min(0.05f)] private float _slideSpeedMps = 0.5f;

        [Tooltip("박스가 서서 뚜껑을 기다리다 경고를 내기까지의 시간(초, 시뮬레이션 시간). 지나도 박스는 계속 기다린다.")]
        [SerializeField, Min(1f)] private float _lidWaitWarnSeconds = 120f;

        // 멈출 자리에 이 거리(m) 안으로 들어오면 미끄러짐을 끝낸다.
        private const float SlideDoneM = 0.001f;

        private KitBoxCarrier _arriving;
        private KitBoxCarrier _held;
        private float _heldSince;
        private bool _timeoutWarned;

        private void Awake()
        {
            if (_conveyor == null || _packingSensor == null)
            {
                Debug.LogError($"{name}: Conveyor(PauserConveyor)와 Packing Sensor를 모두 지정해야 한다.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _conveyor.ParcelEntered += OnParcelEntered;
            CellEvents.LidClosed += OnLidClosed;
        }

        private void OnDisable()
        {
            _conveyor.ParcelEntered -= OnParcelEntered;
            CellEvents.LidClosed -= OnLidClosed;
        }

        private void Update()
        {
            if (_arriving != null)
            {
                // 컨베이어의 코루틴은 Update가 끝난 뒤에 이어지므로, 자리를 지난 바로 다음 프레임에 여기서 멈춰도 구간이 먼저 더 나아가지 못한다.
                Vector3 stop = StopPosition;
                Vector3 flow = Flow;
                if (StopPointArrival.HasReached(_arriving.transform.position, stop, flow))
                {
                    Arrive(_arriving, stop, flow);
                    _arriving = null;
                }

                return;
            }

            WarnIfLidTakesTooLong();
        }

        private Vector3 StopPosition => _stopPoint != null ? _stopPoint.position : _conveyor.CenterPoint.position;

        // 구간의 진행 방향: 입력점에서 출력점으로.
        private Vector3 Flow => _conveyor.OutputPoints[0].position - _conveyor.InputPoint.position;

        private void OnParcelEntered(ConveyorBase conveyor, Parcel parcel)
        {
            // 이 구간에는 키트 박스가 아닌 물건이 들어올 수도 있다. 우리가 보낸 박스만 세운다.
            var carrier = parcel.GetComponent<KitBoxCarrier>();
            if (carrier != null)
            {
                _arriving = carrier;
            }
        }

        // 자리를 지나간 프레임에 구간부터 멈추고(그래야 컨베이어가 더 옮기지 못한다), 진행 방향을 자리에 맞춘 뒤 옆으로 미끄러뜨린다.
        private void Arrive(KitBoxCarrier carrier, Vector3 stop, Vector3 flow)
        {
            _conveyor.StopConveyor();
            carrier.transform.position = StopPointArrival.SnapToStopPoint(carrier.transform.position, stop, flow);
            StartCoroutine(SlideToStopPoint(carrier));
        }

        // 구간이 멈춰 있는 동안 박스를 벨트 중심선에서 멈출 자리로 옮긴다. 높이는 벨트가 정한 그대로 둔다.
        private IEnumerator SlideToStopPoint(KitBoxCarrier carrier)
        {
            Transform carrierTransform = carrier.transform;
            Vector3 target = StopPosition;
            target.y = carrierTransform.position.y;

            while ((carrierTransform.position - target).magnitude > SlideDoneM)
            {
                carrierTransform.position = Vector3.MoveTowards(carrierTransform.position, target, _slideSpeedMps * Time.deltaTime);
                yield return null;
            }

            carrierTransform.position = target;
            Hold(carrier);
        }

        private void Hold(KitBoxCarrier carrier)
        {
            _held = carrier;
            _heldSince = Time.time;
            _timeoutWarned = false;

            carrier.transform.SetParent(_packingSensor, true);
            CellEvents.RaiseAtPacking(carrier.Kit);
        }

        private void OnLidClosed(KitData kit)
        {
            if (_held == null)
            {
                return;
            }

            _held.transform.SetParent(_conveyor.transform, true);
            _held = null;
            _conveyor.StartConveyor();
        }

        private void WarnIfLidTakesTooLong()
        {
            if (_held == null || _timeoutWarned || Time.time - _heldSince < _lidWaitWarnSeconds)
            {
                return;
            }

            _timeoutWarned = true;
            CellEvents.RaiseWarning(ConveyorWarningCodes.PackingTimeout, $"키트 {_held.Kit?.KitSequence}의 박스가 팩킹 구역에서 {_lidWaitWarnSeconds:0}초 넘게 뚜껑을 기다립니다.");
        }
    }
}
