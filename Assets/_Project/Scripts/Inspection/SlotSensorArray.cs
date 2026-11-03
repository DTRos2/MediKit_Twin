using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.Inspection
{
    /// <summary>박스 하나의 슬롯 센서 묶음. 박스 프리팹의 루트에 두고, 자식 SlotSensor를 슬롯 번호로 찾아 한꺼번에 읽는다.</summary>
    public sealed class SlotSensorArray : MonoBehaviour, ISlotReader
    {
        public bool TryRead(int slotCount, out string[] componentIds)
        {
            componentIds = null;

            var bySlot = new Dictionary<int, SlotSensor>();
            foreach (SlotSensor sensor in GetComponentsInChildren<SlotSensor>())
            {
                bySlot[sensor.SlotNumber] = sensor;
            }

            var result = new string[slotCount];
            for (int slot = 1; slot <= slotCount; slot++)
            {
                if (!bySlot.TryGetValue(slot, out SlotSensor sensor) || !sensor.IsResponsive)
                {
                    return false;
                }

                result[slot - 1] = sensor.Read();
            }

            componentIds = result;
            return true;
        }
    }
}
