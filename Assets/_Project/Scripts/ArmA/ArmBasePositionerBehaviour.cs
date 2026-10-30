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

        [Header("이동 전 팔 접기 (둘 다 비우면 팔을 편 채 이동)")]
        [Tooltip("도구 끝점 목표를 받는 쪽(IToolPoseTarget 구현체, 보통 IkArmTarget). 접기 전에 남은 IK 목표를 지운다.")]
        [SerializeField] private MonoBehaviour _toolTarget;
        [Tooltip("관절 목표를 주는 쪽(IArmDriver 구현체, 보통 ArmController).")]
        [SerializeField] private MonoBehaviour _armDriver;

        private IArmBasePositioner _positioner;

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
                mobile,
                locator,
                new MountedBaseGoalPlanner(_config.MinHeadingTravelM),
                () => SimClock.Now,
                _config.ArriveToleranceM,
                _config.MaxCorrections,
                _config.TimeoutSeconds,
                _config.SettleFrames);

            // 안쪽부터: 이동 → (이동 중 충돌 끄기) → (이동 전 팔 접기). 접을 때는 충돌이 켜져 있어야 접다가 걸리는 것도 드러난다.
            IMotionScope collisionScope = CreateCollisionScope();
            if (collisionScope != null)
            {
                _positioner = new ScopedArmBasePositioner(_positioner, collisionScope);
            }

            IArmStower stower = CreateStower();
            if (stower != null)
            {
                _positioner = new StowingArmBasePositioner(_positioner, stower);
            }
        }

        // 두 레이어 이름이 모두 프로젝트에 정의되어 있어야 끌 수 있다. 이름이 비었으면 쓰지 않는 설정이고, 이름은 있는데 없는 레이어면 설정 실수이므로 알린다.
        private IMotionScope CreateCollisionScope()
        {
            if (string.IsNullOrEmpty(_config.ArmLayerName) || string.IsNullOrEmpty(_config.ShelfLayerName))
            {
                return null;
            }

            int armLayer = LayerMask.NameToLayer(_config.ArmLayerName);
            int shelfLayer = LayerMask.NameToLayer(_config.ShelfLayerName);
            if (armLayer < 0 || shelfLayer < 0)
            {
                Debug.LogWarning(
                    $"{name}: 레이어 '{_config.ArmLayerName}' 또는 '{_config.ShelfLayerName}'이 프로젝트에 없어 이동 중 충돌을 끄지 않는다. Tools > MediKit > Setup Kitting Station으로 만든다.",
                    this);
                return null;
            }

            return new LayerCollisionIgnoreScope(armLayer, shelfLayer);
        }

        // 도구 목표와 팔 드라이버가 둘 다 있어야 접을 수 있다. 하나만 있으면 설정 실수이므로 알린다.
        private IArmStower CreateStower()
        {
            var tool = _toolTarget as IToolPoseTarget;
            var driver = _armDriver as IArmDriver;
            if (tool == null || driver == null)
            {
                Debug.LogWarning($"{name}: Tool Target(IToolPoseTarget)과 Arm Driver(IArmDriver)가 없어 팔을 편 채 이동한다. 선반에 걸릴 수 있다.", this);
                return null;
            }

            return new ArmStowMover(tool, driver, () => SimClock.Now, _config.StowToleranceRad, _config.StowTimeoutSeconds);
        }

        public string LastFailureReason => _positioner != null ? _positioner.LastFailureReason : string.Empty;

        public IEnumerator MoveArmBaseTo(Vector3 worldPosition, Action<bool> done)
        {
            return _positioner.MoveArmBaseTo(worldPosition, done);
        }
    }
}
