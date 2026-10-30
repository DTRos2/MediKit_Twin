using System;
using System.Collections;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// IMobileBase(굴러가는 베이스)로 팔 베이스를 목표 지점에 가져다 놓는다.
    /// 베이스 루트가 목표에 닿아도 회전하면 팔 베이스가 루트에서 떨어진 만큼 어긋나므로, 팔 베이스의 오차를 재서
    /// 그만큼 루트를 다시 보내는 보정을 정해진 횟수까지 되풀이한다. 도착을 못 하거나 수렴하지 못하면 실패로 끝낸다.
    /// 시간은 주입받아 시험에서 흐름을 직접 정한다.
    /// </summary>
    public sealed class MobileArmBasePositioner : IArmBasePositioner
    {
        private readonly IMobileBase _mobile;
        private readonly IArmBaseLocator _armBase;
        private readonly Func<float> _now;
        private readonly float _toleranceM;
        private readonly int _maxCorrections;
        private readonly float _timeoutSeconds;

        public MobileArmBasePositioner(
            IMobileBase mobile,
            IArmBaseLocator armBase,
            Func<float> now,
            float toleranceM,
            int maxCorrections,
            float timeoutSeconds)
        {
            _mobile = mobile ?? throw new ArgumentNullException(nameof(mobile));
            _armBase = armBase ?? throw new ArgumentNullException(nameof(armBase));
            _now = now ?? throw new ArgumentNullException(nameof(now));
            _toleranceM = toleranceM;
            _maxCorrections = maxCorrections;
            _timeoutSeconds = timeoutSeconds;
        }

        public IEnumerator MoveArmBaseTo(Vector3 worldPosition, Action<bool> done)
        {
            // 처음 이동 1번에 보정 최대 횟수를 더한 만큼 보낸다.
            for (int attempt = 0; attempt <= _maxCorrections; attempt++)
            {
                Vector3 error = FlatError(worldPosition);
                if (error.magnitude <= _toleranceM)
                {
                    done(true);
                    yield break;
                }

                _mobile.DriveTo(_mobile.Position + error);

                float startedAt = _now();
                while (_mobile.IsMoving)
                {
                    if (_now() - startedAt > _timeoutSeconds)
                    {
                        _mobile.Stop();
                        done(false);
                        yield break;
                    }

                    yield return null;
                }
            }

            done(FlatError(worldPosition).magnitude <= _toleranceM);
        }

        // 팔 베이스에서 목표까지의 수평 오차. 높이는 베이스가 바꿀 수 없으므로 뺀다.
        private Vector3 FlatError(Vector3 target)
        {
            Vector3 difference = target - _armBase.BaseWorldPosition;
            return new Vector3(difference.x, 0f, difference.z);
        }
    }
}
