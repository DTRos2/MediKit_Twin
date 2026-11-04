using ConveyorSystem;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 박스가 컨베이어 끝에 닿으면 AtConveyorEnd를 알린다. (FR-05)
    /// 마지막 구간 뒤에는 이어진 구간이 없어서, 컨베이어 에셋은 박스를 그 구간 가운데에서 "막힘"으로 세운다.
    /// 그 막힘이 곧 끝에 닿았다는 뜻이므로 에셋의 ParcelBlocked를 그대로 쓴다. 하역 로봇이 박스를 가져가기 전까지 박스는 거기 서 있다.
    /// 끝에 받침대가 있어 ConveyorEndRunout이 박스를 받침대까지 보내는 씬에서는, 박스가 받침대에 앉은 뒤에 알린다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConveyorEndNotifier : MonoBehaviour
    {
        [Tooltip("컨베이어의 마지막 구간. 뒤에 이어진 구간이 없어야 한다.")]
        [SerializeField] private ConveyorBase _lastConveyor;

        [Tooltip("지정하면 박스가 받침대에 앉은 뒤에 알린다. 비우면 마지막 구간에서 막히는 즉시 알린다.")]
        [SerializeField] private ConveyorEndRunout _runout;

        private void Awake()
        {
            if (_lastConveyor == null)
            {
                Debug.LogError($"{name}: Last Conveyor를 지정해야 한다.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_runout != null)
            {
                _runout.BoxSeated += OnBoxSeated;
            }
            else
            {
                _lastConveyor.ParcelBlocked += OnParcelBlocked;
            }
        }

        private void OnDisable()
        {
            if (_runout != null)
            {
                _runout.BoxSeated -= OnBoxSeated;
            }
            else
            {
                _lastConveyor.ParcelBlocked -= OnParcelBlocked;
            }
        }

        private void OnBoxSeated(KitBoxCarrier carrier)
        {
            CellEvents.RaiseAtConveyorEnd(carrier.Kit);
        }

        private void OnParcelBlocked(ConveyorBase conveyor, Parcel parcel)
        {
            var carrier = parcel.GetComponent<KitBoxCarrier>();
            if (carrier != null)
            {
                CellEvents.RaiseAtConveyorEnd(carrier.Kit);
            }
        }
    }
}
