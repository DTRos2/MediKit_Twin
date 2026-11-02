using System;
using MediKitTwin.Common;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 팔 베이스를 옮기는 동안 스텝마다 재는 데 필요한 것을 묶은 꾸러미: 스텝을 알려 주는 쪽, 굴러가는 베이스, 시계, 밀림 기준.
    /// 씬에서 한 번 조립해 두고, 물건 하나를 놓을 때마다 BaseMotionRecorder가 이것을 받아 만들어진다.
    /// </summary>
    public sealed class BaseMotionProbe
    {
        /// <param name="steps">물리 스텝마다 알려 주는 쪽.</param>
        /// <param name="mobile">굴러가는 베이스. 자리와 방향을 읽는다.</param>
        /// <param name="now">지금 시각(초). 스텝 사이의 속도를 구하는 데 쓰므로 스텝마다 달라져야 한다.</param>
        /// <param name="onsetSlipM">쥔 물건이 이만큼(m) 밀리면 "밀리기 시작했다"고 보고 그때의 속도를 남긴다.</param>
        public BaseMotionProbe(IPhysicsStepSource steps, IMobileBase mobile, Func<float> now, float onsetSlipM)
        {
            Steps = steps ?? throw new ArgumentNullException(nameof(steps));
            Mobile = mobile ?? throw new ArgumentNullException(nameof(mobile));
            Now = now ?? throw new ArgumentNullException(nameof(now));
            OnsetSlipM = onsetSlipM;
        }

        public IPhysicsStepSource Steps { get; }

        public IMobileBase Mobile { get; }

        public Func<float> Now { get; }

        public float OnsetSlipM { get; }
    }
}
