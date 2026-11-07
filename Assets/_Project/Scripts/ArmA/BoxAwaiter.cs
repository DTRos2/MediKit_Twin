using System;
using System.Collections;
using MediKitTwin.Common;
using MediKitTwin.Recipes;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 지그에 박스를 놓고 슬롯 위치를 알 수 있을 때까지 기다린다. 박스가 이미 있으면 바로 끝난다.
    /// 시간은 주입받으므로 시뮬레이션 시간(배속)을 따르고, 시험에서 흐름을 직접 정한다.
    /// </summary>
    public sealed class BoxAwaiter
    {
        private readonly IBoxSupply _supply;
        private readonly IKitBoxLocator _locator;
        private readonly Func<float> _now;
        private readonly float _timeoutSeconds;

        public BoxAwaiter(IBoxSupply supply, IKitBoxLocator locator, Func<float> now, float timeoutSeconds)
        {
            _supply = supply ?? throw new ArgumentNullException(nameof(supply));
            _locator = locator ?? throw new ArgumentNullException(nameof(locator));
            _now = now ?? throw new ArgumentNullException(nameof(now));
            _timeoutSeconds = timeoutSeconds;
        }

        /// <summary>박스가 준비되면 done(true), 시간 안에 놓이지 않으면 done(false)를 부른다.</summary>
        public IEnumerator Ensure(KitData kit, BoxSize size, Action<bool> done)
        {
            _supply.PlaceBox(kit, size);

            float deadline = _now() + _timeoutSeconds;
            while (!_locator.TryGetSlotTarget(Vector3.zero, out _))
            {
                if (_now() > deadline)
                {
                    done(false);
                    yield break;
                }

                yield return null;
            }

            done(true);
        }
    }
}
