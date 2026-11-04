using System.Collections.Generic;
using ConveyorSystem;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 팩킹 구역에서 뚜껑 덮기가 실패하면 컨베이어와 팔 A를 바로 멈추고, 뚜껑이 덮이면(LidClosed) 그 자리에서 다시 돌린다. (FR-10)
    /// 컨베이어는 씬의 모든 구간(ConveyorBase)의 이송 속도를 0으로 만들고 재개할 때 원래 속도로 돌려놓는다. 박스는 그 자리에 서고 벨트 무늬도 멈춘다.
    /// 팔 A는 IStationFreeze로 하던 키트 동작 중간에 멈춘다.
    /// 실패한 박스는 PackingStop이 팩킹 구역에 그대로 세워 둔다. 다시 덮는 일은 사용자가 확인한 뒤 팔 B(LidCoverTask)가 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PackingFailureHandler : MonoBehaviour
    {
        private readonly Dictionary<ConveyorBase, float> _savedSpeeds = new Dictionary<ConveyorBase, float>();
        private IStationFreeze _armA;
        private bool _isHalted;

        private void OnEnable()
        {
            CellEvents.PackingFailed += OnPackingFailed;
            CellEvents.LidClosed += OnLidClosed;
        }

        private void OnDisable()
        {
            CellEvents.PackingFailed -= OnPackingFailed;
            CellEvents.LidClosed -= OnLidClosed;
        }

        private void OnPackingFailed(KitData kit, string reason)
        {
            // 다시 시도해서 또 실패해도 이미 멈춰 있으므로 한 번만 멈춘다.
            if (_isHalted)
            {
                return;
            }

            _isHalted = true;
            foreach (ConveyorBase conveyor in FindObjectsByType<ConveyorBase>(FindObjectsInactive.Include))
            {
                _savedSpeeds[conveyor] = conveyor.Speed;
                conveyor.Speed = 0f;
            }

            _armA = FindArmA();
            _armA?.Freeze();
        }

        private void OnLidClosed(KitData kit)
        {
            if (!_isHalted)
            {
                return;
            }

            _isHalted = false;
            foreach (KeyValuePair<ConveyorBase, float> saved in _savedSpeeds)
            {
                if (saved.Key != null)
                {
                    saved.Key.Speed = saved.Value;
                }
            }

            _savedSpeeds.Clear();
            _armA?.Unfreeze();
            _armA = null;
        }

        private static IStationFreeze FindArmA()
        {
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude))
            {
                if (behaviour is IStationFreeze pause)
                {
                    return pause;
                }
            }

            return null;
        }
    }
}
