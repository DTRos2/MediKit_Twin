#if UNITY_EDITOR
using System.Collections.Generic;
using ConveyorSystem;
using MediKitTwin.Common;
using MediKitTwin.Conveyor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MediKitTwin.PlayMode.Tests
{
    /// <summary>
    /// 하역 로봇의 대역. 하역 로봇은 아직 구현되지 않아 컨베이어 끝에 닿은 박스가 그대로 서 있고, 뒤따르는 박스는 끝에 닿지 못하고 앞에서 기다린다.
    /// 끝에 닿은 박스를 정해진 시간 뒤에 컨베이어에서 치우고 KitShipped를 보내, 키트가 여럿 지나가는 공정을 시험할 수 있게 한다.
    /// 시험 루프가 매 프레임 Tick을 불러 준다.
    /// </summary>
    internal sealed class EndOfLineUnloaderStub
    {
        private readonly float _holdSeconds;
        private readonly HashSet<KitData> _shipped = new HashSet<KitData>();

        /// <param name="holdSeconds">끝에 닿은 박스를 그대로 두는 시뮬레이션 시간(초).</param>
        public EndOfLineUnloaderStub(float holdSeconds)
        {
            _holdSeconds = holdSeconds;
        }

        /// <summary>하역한 키트 수.</summary>
        public int ShippedCount => _shipped.Count;

        public void Tick(ConveyorEventRecorder events)
        {
            foreach (FlowEvent flow in events.Timeline)
            {
                if (flow.Step == FlowStep.AtConveyorEnd && !_shipped.Contains(flow.Kit) && Time.time - flow.Time >= _holdSeconds)
                {
                    Ship(flow.Kit);
                }
            }
        }

        private void Ship(KitData kit)
        {
            _shipped.Add(kit);
            foreach (KitBoxCarrier carrier in Object.FindObjectsByType<KitBoxCarrier>())
            {
                if (carrier.Kit != kit)
                {
                    continue;
                }

                // 컨베이어가 박스를 놓아야 뒤에서 기다리던 박스가 나아간다.
                ConveyorBase conveyor = carrier.Parcel.CurrentConveyor;
                if (conveyor != null)
                {
                    conveyor.RemoveParcel(carrier.Parcel);
                }

                Object.Destroy(carrier.gameObject);
            }

            kit.CompleteTime = Time.time;
            CellEvents.RaiseKitShipped(kit);
        }
    }
}
#endif
