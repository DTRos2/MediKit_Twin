using System;
using System.Collections;
using MediKitTwin.Common;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// MobileArmBasePositioner를 씬에 붙이는 얇은 컴포넌트. 굴러가는 베이스(IMobileBase)와 팔 베이스 위치(IArmBaseLocator),
    /// 설정을 받아 순수 클래스를 만들고 IArmBasePositioner로 내놓는다. 이동 판단은 여기가 아니라 순수 클래스가 한다.
    /// </summary>
    public sealed class ArmBasePositionerBehaviour : MonoBehaviour, IArmBasePositioner
    {
        [Tooltip("굴러가는 베이스(IMobileBase 구현체, 보통 ArmA의 MobileBaseDriver).")]
        [SerializeField] private MonoBehaviour _mobileBase;
        [Tooltip("팔 베이스 위치를 알려 주는 쪽(IArmBaseLocator 구현체, 보통 ArmController).")]
        [SerializeField] private MonoBehaviour _armBase;
        [SerializeField] private ArmBaseApproachConfigSO _config;

        private MobileArmBasePositioner _positioner;

        private void Awake()
        {
            var mobile = _mobileBase as IMobileBase;
            var locator = _armBase as IArmBaseLocator;
            if (mobile == null || locator == null || _config == null)
            {
                Debug.LogError($"{name}: Mobile Base(IMobileBase), Arm Base(IArmBaseLocator), Config를 모두 지정해야 한다.", this);
                enabled = false;
                return;
            }

            _positioner = new MobileArmBasePositioner(
                mobile, locator, () => SimClock.Now, _config.ArriveToleranceM, _config.MaxCorrections, _config.TimeoutSeconds);
        }

        public IEnumerator MoveArmBaseTo(Vector3 worldPosition, Action<bool> done)
        {
            return _positioner.MoveArmBaseTo(worldPosition, done);
        }
    }
}
