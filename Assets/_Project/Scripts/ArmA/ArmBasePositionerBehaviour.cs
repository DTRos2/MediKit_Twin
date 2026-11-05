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
    public sealed class ArmBasePositionerBehaviour : MonoBehaviour, IArmBasePositioner, IArmBaseStateSource, IArmStowPoseSource, IArmPhaseSource
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
        private IArmDriver _stowDriver;

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
                _config.SettleFrames,
                _config.FacingToleranceDeg);

            // 안쪽부터: 이동 → (이동 중 충돌 끄기) → (이동 전 팔 접기). 접을 때는 충돌이 켜져 있어야 접다가 걸리는 것도 드러난다.
            IMotionScope collisionScope = CreateCollisionScope();
            if (collisionScope != null)
            {
                _positioner = new ScopedArmBasePositioner(_positioner, collisionScope);
            }

            IArmStower stower = CreateStower();
            if (stower != null)
            {
                var stowing = new StowingArmBasePositioner(_positioner, stower);
                stowing.PhaseReached += phase => PhaseReached?.Invoke(phase);
                _positioner = stowing;
            }
        }

        /// <summary>이동 전에 팔을 접도록 되어 있을 때만 단계(접었다, 도착했다)를 알린다. 팔을 편 채 이동하면 알릴 것이 없다.</summary>
        public event Action<ArmMotionPhase> PhaseReached;

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

            _stowDriver = driver;
            return new ArmStowMover(tool, driver, () => SimClock.Now, _config.StowToleranceRad, _config.StowTimeoutSeconds);
        }

        /// <summary>이동 전에 팔을 접도록 되어 있으면 그 접는 자세(시작 자세)를 새 배열로 준다. 접지 않으면 false.</summary>
        public bool TryGetStowPose(out float[] jointsRad)
        {
            jointsRad = null;
            if (_stowDriver == null)
            {
                return false;
            }

            float[] home = _stowDriver.HomePoseRad;
            jointsRad = (float[])home.Clone();
            return true;
        }

        /// <summary>
        /// 팔 베이스 링크의 월드 자세와 베이스 루트의 방향을 알려 준다. 팔 모델(IArmModel)이 아니거나 아직 준비되지 않았으면 false다.
        /// 서 보기 전에 서는 자리에서 팔이 닿을지 가늠하는 쪽이 쓴다.
        /// </summary>
        public bool TryGetState(out Pose armBaseWorldPose, out float rootYawDeg)
        {
            armBaseWorldPose = default;
            rootYawDeg = 0f;

            var mobile = _mobileBase as IMobileBase;
            var arm = _armBase as IArmModel;
            if (mobile == null || arm == null || !arm.IsReady || arm.BaseLink == null)
            {
                return false;
            }

            armBaseWorldPose = new Pose(arm.BaseLink.position, arm.BaseLink.rotation);
            rootYawDeg = mobile.YawDeg;
            return true;
        }

        /// <summary>이 이동 모듈이 옮기는 굴러가는 베이스. 지정되지 않았거나 IMobileBase가 아니면 null이다.</summary>
        public IMobileBase MobileBase => _mobileBase as IMobileBase;

        /// <summary>팔 베이스 위치를 알려 주는 쪽(보통 ArmController). 어느 팔이 이 베이스에 실려 있는지 알아내는 데 쓴다.</summary>
        public MonoBehaviour ArmBaseSource => _armBase;

        public string LastFailureReason => _positioner != null ? _positioner.LastFailureReason : string.Empty;

        public IEnumerator MoveArmBaseTo(Vector3 worldPosition, Action<bool> done)
        {
            return _positioner.MoveArmBaseTo(worldPosition, done);
        }

        public IEnumerator MoveArmBaseTo(Vector3 worldPosition, float facingYawDeg, Action<bool> done)
        {
            return _positioner.MoveArmBaseTo(worldPosition, facingYawDeg, done);
        }
    }
}
