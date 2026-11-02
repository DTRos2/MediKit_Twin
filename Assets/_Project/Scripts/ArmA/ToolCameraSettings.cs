using System;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 도구 끝에 다는 카메라의 설정. 위치와 방향은 도구 끝점(TCP) 기준이고, TCP의 +Z가 물건 쪽이다.
    /// 기본은 그리퍼 가운데(손 몸통 앞끝 한가운데)에 달고 앞을 똑바로 본다. 손 몸통 옆면에 다는 것도 고를 수 있다(ToolCameraMount). 몸통 크기는 도구 링크의 충돌체에서 재므로 로봇이 달라도 맞는다.
    /// 자리가 어긋나면 Nudge로 옮기거나, 옆면을 바꾸거나, Manual로 위치와 방향을 직접 적는다.
    /// </summary>
    [Serializable]
    public sealed class ToolCameraSettings
    {
        [Tooltip("끄면 카메라를 달지 않는다. 카메라가 없으면 물건 인식도 하지 않는다.")]
        [SerializeField] private bool _enabled = true;
        [Tooltip("켜면(기본) 카메라 화면을 실제로 그리고(RenderTexture), 물건을 인식할 때 화면 왼쪽 아래 'Tool Camera' 창에 보여 준다. 끄면 화면 없이 카메라 자세와 시야만 쓴다. 물건 인식은 어느 쪽이든 되고, 인식한 자리는 메인 화면에 초록 표시로 보인다.")]
        [SerializeField] private bool _renderScreen = true;
        [Tooltip("카메라를 달 자리. 그리퍼 가운데(앞을 똑바로 봄), 손 몸통 옆면(오른쪽 +X, 왼쪽 −X), 또는 아래 Local Position과 Local Euler를 그대로 쓰는 Manual.")]
        [SerializeField] private ToolCameraMount _mount = ToolCameraMount.HandCenter;
        [Tooltip("몸통 옆면에서 바깥으로 띄우는 거리(m). 0이면 면에 붙는다.")]
        [SerializeField, Min(0f)] private float _surfaceOffsetM = 0.005f;
        [Tooltip("몸통 뒤끝(0)에서 앞끝(1) 사이 어디에 달지. 기본 1은 몸통 앞끝(손가락이 시작하는 곳)이다. 옆면에 달 때는 0.65쯤이 몸통 가운데 아래쪽이다.")]
        [SerializeField, Range(0f, 1f)] private float _forwardFraction = 1f;
        [Tooltip("켜면 카메라 자리에 작은 카메라 본체(상자와 렌즈)가 눈에 보인다. 카메라 화면에는 나오지 않고 충돌체도 없다. 그리퍼 가운데에 달면 본체가 손 몸통 안에 들어가 밖에서는 안 보인다.")]
        [SerializeField] private bool _showCameraBody = true;
        [Tooltip("도구 끝점에서 앞으로 이만큼 떨어진 축 위의 점을 본다(m). 접근 자리에서 물건까지의 거리쯤이 알맞다.")]
        [SerializeField, Min(0.05f)] private float _lookAheadM = 0.15f;
        [Tooltip("몸통 옆면에 단 자리에서 도구 끝점 좌표(X 옆, Y 위, Z 앞)로 더 옮길 양(m).")]
        [SerializeField] private Vector3 _nudge = Vector3.zero;
        [Tooltip("Manual일 때, 또는 몸통 크기를 재지 못했을 때 쓰는 도구 끝점(TCP) 기준 카메라 위치(m). 앞(+Z)이 물건 쪽이다.")]
        [SerializeField] private Vector3 _localPosition = new Vector3(0f, 0.05f, -0.08f);
        [Tooltip("Manual일 때, 또는 몸통 크기를 재지 못했을 때 쓰는 도구 끝점(TCP) 기준 카메라 방향(오일러 각, 도). 0이면 도구와 같은 쪽을 본다.")]
        [SerializeField] private Vector3 _localEulerDeg = Vector3.zero;
        [Tooltip("켜면 이 카메라 화면에서만 로봇 몸(손, 손가락, 팔)을 그리지 않아 앞이 가려지지 않는다. 메인 화면에는 로봇이 그대로 보이고 충돌에는 영향이 없다. 손가락을 화면에서 보고 싶으면 끈다.")]
        [SerializeField] private bool _hideOwnBody = true;
        [Tooltip("세로 시야각(도).")]
        [SerializeField, Range(20f, 120f)] private float _verticalFovDeg = 70f;
        [Tooltip("화면 해상도(px). 키우면 선명하지만 매 프레임 그리는 비용이 는다.")]
        [SerializeField] private Vector2Int _resolution = new Vector2Int(320, 240);
        [Tooltip("이보다 가까운 것은 보이지 않는다(m).")]
        [SerializeField, Min(0.005f)] private float _nearM = 0.02f;
        [Tooltip("이보다 먼 것은 보이지 않는다(m).")]
        [SerializeField, Min(0.1f)] private float _farM = 2f;

        public bool Enabled => _enabled;

        public ToolCameraMount Mount => _mount;

        public float SurfaceOffsetM => _surfaceOffsetM;

        public float ForwardFraction => _forwardFraction;

        public float LookAheadM => _lookAheadM;

        public Vector3 Nudge => _nudge;

        public Vector3 LocalPosition => _localPosition;

        public Quaternion LocalRotation => Quaternion.Euler(_localEulerDeg);

        public bool RenderImage => _renderScreen;

        public bool HideOwnBody => _hideOwnBody;

        public bool ShowCameraBody => _showCameraBody;

        public float VerticalFovDeg => _verticalFovDeg;

        public int Width => Mathf.Max(32, _resolution.x);

        public int Height => Mathf.Max(32, _resolution.y);

        public float NearM => _nearM;

        public float FarM => Mathf.Max(_nearM + 0.01f, _farM);
    }
}
