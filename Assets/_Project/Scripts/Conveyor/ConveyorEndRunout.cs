using System;
using System.Collections;
using ConveyorSystem;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 마지막 구간 끝에서 박스를 받침대까지 보낸다. (FR-05, FR-12)
    /// 컨베이어 에셋은 뒤에 이어진 구간이 없으면 박스를 그 구간 가운데에서 "막힘"으로 세운다. 컨베이어 끝에는 하역 로봇이 박스를 집는
    /// 받침대가 있으므로, 막힌 박스를 구간 속도로 받침대 위까지 옮기고 받침대 윗면에 내려앉힌 뒤 컨베이어에서 풀어 준다.
    /// 받침대에 아직 앞 박스가 있으면(하역 로봇이 안 가져갔으면) 박스는 구간 가운데에서 기다린다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConveyorEndRunout : MonoBehaviour
    {
        private const int MaxOverlaps = 16;

        [Tooltip("컨베이어의 마지막 구간. 뒤에 이어진 구간이 없어야 한다.")]
        [SerializeField] private ConveyorBase _lastConveyor;

        [Tooltip("박스가 놓일 자리. 박스 바닥 중앙이 이 점의 수평 위치에 오고, 이 점 아래의 받침대 윗면에 앉는다.")]
        [SerializeField] private Transform _seatPoint;

        [Tooltip("받침대 윗면을 찾으려고 자리 위쪽 몇 m에서 아래로 쏘는지. 자리보다 위여야 한다.")]
        [SerializeField, Min(0.05f)] private float _surfaceProbeHeightM = 0.5f;

        [Tooltip("받침대 윗면으로 볼 물리 레이어.")]
        [SerializeField] private LayerMask _surfaceLayers = ~0;

        [Tooltip("받침대에 앞 박스가 있는지 볼 때 박스 크기에 더하는 여유(m).")]
        [SerializeField, Min(0f)] private float _seatClearMarginM = 0.01f;

        private readonly Collider[] _overlaps = new Collider[MaxOverlaps];
        private KitBoxCarrier _current;

        /// <summary>박스가 받침대 위에 앉고 컨베이어에서 풀렸다. 박스의 키트는 carrier.Kit이다.</summary>
        public event Action<KitBoxCarrier> BoxSeated;

        public Transform SeatPoint => _seatPoint;

        private void Awake()
        {
            if (_lastConveyor == null || _seatPoint == null)
            {
                Debug.LogError($"{name}: Last Conveyor와 Seat Point를 지정해야 한다.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _lastConveyor.ParcelBlocked += OnParcelBlocked;
        }

        private void OnDisable()
        {
            _lastConveyor.ParcelBlocked -= OnParcelBlocked;
            _current = null;
        }

        private void OnParcelBlocked(ConveyorBase conveyor, Parcel parcel)
        {
            var carrier = parcel.GetComponent<KitBoxCarrier>();
            if (carrier == null || carrier == _current)
            {
                return;
            }

            _current = carrier;
            StartCoroutine(Run(carrier));
        }

        private IEnumerator Run(KitBoxCarrier carrier)
        {
            while (carrier != null && !IsSeatClear(carrier))
            {
                yield return null;
            }

            if (carrier == null)
            {
                yield break;
            }

            Transform pivot = carrier.transform;
            Vector3 seat = _seatPoint.position;

            // 앉을 높이는 출발 전에 잰다. 움직인 뒤에는 박스가 자리 위에 있어 위에서 쏘면 박스가 먼저 맞는다.
            float landingY = pivot.position.y;
            if (TryFindSurfaceHeight(seat, out float surfaceY))
            {
                float pivotAboveBottom = pivot.position.y - BoxPivot.MeasureBounds(carrier.Box).min.y;
                landingY = surfaceY + pivotAboveBottom;
            }
            else
            {
                Debug.LogWarning($"{name}: 자리 아래에서 받침대 윗면을 찾지 못해 박스를 내리지 않는다.", this);
            }

            var overSeat = new Vector3(seat.x, pivot.position.y, seat.z);
            while (carrier != null && pivot.position != overSeat)
            {
                pivot.position = Vector3.MoveTowards(pivot.position, overSeat, _lastConveyor.Speed * Time.deltaTime);
                yield return null;
            }

            float fallSpeed = 0f;
            while (carrier != null && pivot.position.y > landingY)
            {
                float y = RunoutFall.Step(pivot.position.y, ref fallSpeed, landingY, Physics.gravity.y, Time.deltaTime);
                pivot.position = new Vector3(overSeat.x, y, overSeat.z);
                yield return null;
            }

            if (carrier == null)
            {
                yield break;
            }

            // 컨베이어가 박스를 놓아야 뒤따르는 박스가 나아간다.
            carrier.GetComponent<KitBoxParcel>().ReleaseToPhysics();
            _current = null;
            BoxSeated?.Invoke(carrier);
        }

        private bool TryFindSurfaceHeight(Vector3 seat, out float surfaceY)
        {
            Vector3 origin = seat + Vector3.up * _surfaceProbeHeightM;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, _surfaceProbeHeightM * 2f, _surfaceLayers, QueryTriggerInteraction.Ignore))
            {
                surfaceY = hit.point.y;
                return true;
            }

            surfaceY = 0f;
            return false;
        }

        // 자리에 다른 박스가 있으면 비워질 때까지 기다린다. 박스 크기는 기다리는 박스의 것을 쓴다(박스는 한 종류).
        private bool IsSeatClear(KitBoxCarrier carrier)
        {
            Vector3 halfExtents = BoxPivot.MeasureBounds(carrier.Box).extents + Vector3.one * _seatClearMarginM;
            int count = Physics.OverlapBoxNonAlloc(_seatPoint.position, halfExtents, _overlaps, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                // 받침이 사라진 뒤에도 받침대 위 박스(하역 로봇 쪽 KitCargo)를 알아보도록 IKitCarrier로 본다.
                IKitCarrier other = _overlaps[i].GetComponentInParent<IKitCarrier>();
                if (other != null && !ReferenceEquals(other, carrier))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
