using System;
using System.Collections.Generic;
using ConveyorSystem;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 컨베이어 구간들의 이송 속도를 한 값으로 맞춘다. 컨베이어 에셋은 구간마다 Speed를 따로 들고 있고 에셋 폴더는 고치지 않는 것이 원칙이라,
    /// 우리 쪽에서 구간들에 같은 속도를 넣는 일을 여기 한곳에 둔다. 씬이나 구간 프리팹마다 속도를 손으로 고치지 않아도 된다.
    /// </summary>
    public static class ConveyorSpeed
    {
        /// <summary>구간 모두의 속도를 speedMps(m/s)로 맞추고, 바꾼 구간 수를 돌려준다. 0 이하의 속도는 박스가 멈춰 서므로 받지 않는다.</summary>
        public static int Apply(IEnumerable<ConveyorBase> conveyors, float speedMps)
        {
            if (conveyors == null)
            {
                throw new ArgumentNullException(nameof(conveyors));
            }

            if (speedMps <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speedMps), "컨베이어 속도는 0보다 커야 한다.");
            }

            int changed = 0;
            foreach (ConveyorBase conveyor in conveyors)
            {
                if (conveyor == null)
                {
                    continue;
                }

                conveyor.Speed = speedMps;
                changed++;
            }

            return changed;
        }
    }
}
