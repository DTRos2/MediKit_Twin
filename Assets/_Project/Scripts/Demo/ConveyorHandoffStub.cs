using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Demo
{
    /// <summary>
    /// 컨베이어가 만들어지기 전까지 완성된 키트 박스를 지그에서 치우는 임시 장치다.
    /// KitReady를 받으면 박스를 바로 치워 다음 키트가 새 박스에서 시작하게 한다. 컨베이어(FR-05)를 구현하면 이 컴포넌트는 지운다.
    /// 치우는 일은 IBoxSupply로만 하므로 박스 공급부의 구체 클래스를 모른다.
    /// </summary>
    public sealed class ConveyorHandoffStub : MonoBehaviour
    {
        [Tooltip("박스 공급부(IBoxSupply 구현체, 보통 KitBoxSupply).")]
        [SerializeField] private MonoBehaviour _boxSupply;

        private IBoxSupply _supply;

        private void Awake()
        {
            _supply = _boxSupply as IBoxSupply;
            if (_supply == null)
            {
                Debug.LogError($"{name}: Box Supply(IBoxSupply)를 지정해야 한다.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            CellEvents.KitReady += OnKitReady;
        }

        private void OnDisable()
        {
            CellEvents.KitReady -= OnKitReady;
        }

        private void OnKitReady(KitData kit)
        {
            _supply.RemoveBox();
        }
    }
}
