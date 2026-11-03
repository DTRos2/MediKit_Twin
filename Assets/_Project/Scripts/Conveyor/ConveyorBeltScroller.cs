using System.Collections.Generic;
using ConveyorSystem;
using UnityEngine;

namespace MediKitTwin.Conveyor
{
    /// <summary>
    /// 컨베이어 벨트 무늬가 물건과 함께 흐르게 한다(보이는 모양만 바꾼다). 컨베이어 에셋은 물건의 위치만 옮기고 벨트 텍스처는 가만히 두어서
    /// 물건만 미끄러져 가는 것처럼 보이는데, 이 컴포넌트가 구간마다 벨트 텍스처 오프셋을 구간의 속도(Speed)로 밀어 준다.
    /// 에셋의 코드는 건드리지 않고 렌더러의 MaterialPropertyBlock에만 쓰므로 공유 재질은 바뀌지 않는다.
    /// 흐르는 방향과 반복 길이는 벨트 메시의 UV에서 직접 구한다(BeltUvFlow). 멈춘 구간(PauserConveyor가 꺼진 구간)은 무늬도 멈춘다.
    /// 코너 구간은 입구에서 가운데까지의 방향을 흐름으로 본다. 씬이 시작될 때 구간을 한 번 모으므로 구간을 나중에 더하면 다시 모아야 한다.
    /// 벨트 텍스처는 한 축으로만 줄무늬가 있어서, 그 축이 아닌 쪽으로 흐르는 구간(코너)은 메시의 U와 V를 맞바꿔야 무늬가 움직이는 것이 보인다.
    /// </summary>
    public sealed class ConveyorBeltScroller : MonoBehaviour
    {
        // 벨트 텍스처가 걸린 URP 재질의 속성 이름. 타일링과 오프셋은 이 이름 뒤에 _ST를 붙인 속성에 담긴다.
        private const string TexturePropertyName = "_BaseMap";
        private const string TilingOffsetSuffix = "_ST";

        [Tooltip("벨트 윗면으로 칠 면의 법선과 위쪽 방향의 최소 내적. 1에 가까울수록 평평하게 위를 보는 면만 쓴다.")]
        [SerializeField, Range(0.5f, 1f)] private float _minTopDot = 0.9f;

        [Tooltip("벨트 텍스처에서 줄무늬가 바뀌는 UV 축. 구간이 이 축이 아닌 쪽으로 흐르면 무늬를 밀어도 움직임이 보이지 않아, " +
                 "그 구간 메시의 U와 V를 맞바꾼 복사본을 쓴다(코너 구간이 그렇다). 에셋의 BeltTread.png는 U 방향으로 줄무늬가 있다.")]
        [SerializeField] private UvAxis _patternAxis = UvAxis.U;

        private readonly List<Belt> _belts = new List<Belt>();
        private MaterialPropertyBlock _block;
        private int _tilingOffsetId;

        private void Start()
        {
            _block = new MaterialPropertyBlock();
            _tilingOffsetId = Shader.PropertyToID(TexturePropertyName + TilingOffsetSuffix);

            foreach (ConveyorBase conveyor in FindObjectsByType<ConveyorBase>())
            {
                if (Belt.TryCreate(conveyor, TexturePropertyName, _minTopDot, _patternAxis, out Belt belt))
                {
                    _belts.Add(belt);
                }
            }

            if (_belts.Count == 0)
            {
                Debug.LogWarning($"{name}: 벨트 텍스처를 흘릴 컨베이어 구간을 찾지 못해 벨트는 가만히 둔다.", this);
                enabled = false;
            }
        }

        /// <summary>
        /// 벨트 무늬를 흘리는 구간과 그 구간의 흐름(흐름 방향 1m당 UV가 바뀌는 양). Start가 구간을 모은 뒤에 채워진다.
        /// 모든 구간의 흐름이 무늬 축을 따르는지 시험이 확인할 때 쓴다.
        /// </summary>
        public IEnumerable<KeyValuePair<ConveyorBase, Vector2>> Flows
        {
            get
            {
                foreach (Belt belt in _belts)
                {
                    yield return new KeyValuePair<ConveyorBase, Vector2>(belt.Conveyor, belt.UvPerMeter);
                }
            }
        }

        /// <summary>무늬가 바뀌는 UV 축. 구간의 흐름은 이 축을 따라야 무늬가 움직이는 것이 보인다.</summary>
        public UvAxis PatternAxis => _patternAxis;

        private void Update()
        {
            foreach (Belt belt in _belts)
            {
                belt.Advance(Time.deltaTime);
                belt.Apply(_block, _tilingOffsetId);
            }
        }

        // 구간 하나의 벨트 모양. 렌더러, 텍스처 반복(타일링), UV가 흐르는 양, 지금까지 민 오프셋을 가진다.
        private sealed class Belt
        {
            private readonly ConveyorBase _conveyor;
            private readonly Renderer _renderer;
            private readonly Vector2 _tiling;
            private readonly Vector2 _uvPerMeter;
            private readonly BeltScroll _scroll = new BeltScroll();

            private Belt(ConveyorBase conveyor, Renderer renderer, Vector2 tiling, Vector2 uvPerMeter)
            {
                _conveyor = conveyor;
                _renderer = renderer;
                _tiling = tiling;
                _uvPerMeter = uvPerMeter;
            }

            public ConveyorBase Conveyor => _conveyor;

            public Vector2 UvPerMeter => _uvPerMeter;

            public static bool TryCreate(ConveyorBase conveyor, string textureProperty, float minTopDot, UvAxis patternAxis, out Belt belt)
            {
                belt = null;
                var renderer = conveyor.GetComponent<MeshRenderer>();
                var filter = conveyor.GetComponent<MeshFilter>();
                if (renderer == null || filter == null || filter.sharedMesh == null || renderer.sharedMaterial == null
                    || !renderer.sharedMaterial.HasProperty(textureProperty))
                {
                    return false;
                }

                Mesh mesh = filter.sharedMesh;
                if (!mesh.isReadable || conveyor.InputPoint == null || conveyor.CenterPoint == null)
                {
                    return false;
                }

                Vector3 flow = conveyor.CenterPoint.position - conveyor.InputPoint.position;
                flow.y = 0f;
                Vector2 uvPerMeter = BeltUvFlow.PerMeter(WorldPositions(mesh, renderer.transform), WorldNormals(mesh, renderer.transform), mesh.uv, flow, minTopDot);
                if (uvPerMeter == Vector2.zero)
                {
                    return false;
                }

                // 무늬가 없는 축으로 흐르는 구간은 오프셋을 밀어도 눈에 보이는 변화가 없다. 이 구간만 U와 V를 맞바꾼 메시 복사본을 쓴다.
                // 공유 메시(큐브)를 직접 고치면 다른 구간이 함께 바뀌므로, 복사본을 이 구간의 필터에만 건다.
                if (BeltUvAxes.IsAcrossPattern(uvPerMeter, patternAxis))
                {
                    Mesh swapped = Object.Instantiate(mesh);
                    swapped.uv = BeltUvAxes.Swapped(mesh.uv);
                    filter.sharedMesh = swapped;
                    uvPerMeter = BeltUvAxes.Swapped(uvPerMeter);
                }

                belt = new Belt(conveyor, renderer, renderer.sharedMaterial.GetTextureScale(textureProperty), uvPerMeter);
                return true;
            }

            public void Advance(float deltaSeconds)
            {
                _scroll.Advance(_uvPerMeter, IsRunning() ? _conveyor.Speed : 0f, deltaSeconds);
            }

            // 다른 스크립트(강조 표시 등)가 같은 렌더러에 쓴 속성을 지우지 않게, 있던 값을 읽어 와서 오프셋만 바꿔 다시 쓴다.
            public void Apply(MaterialPropertyBlock block, int tilingOffsetId)
            {
                _renderer.GetPropertyBlock(block);
                block.SetVector(tilingOffsetId, new Vector4(_tiling.x, _tiling.y, _scroll.Offset.x, _scroll.Offset.y));
                _renderer.SetPropertyBlock(block);
            }

            // 멈추는 구간(PauserConveyor)이 꺼져 있으면 물건이 서므로 무늬도 선다. 그 밖의 구간은 늘 돈다.
            private bool IsRunning()
            {
                return !(_conveyor is PauserConveyor pauser) || pauser.IsActive;
            }

            private static List<Vector3> WorldPositions(Mesh mesh, Transform transform)
            {
                Vector3[] local = mesh.vertices;
                var world = new List<Vector3>(local.Length);
                foreach (Vector3 vertex in local)
                {
                    world.Add(transform.TransformPoint(vertex));
                }

                return world;
            }

            private static List<Vector3> WorldNormals(Mesh mesh, Transform transform)
            {
                Vector3[] local = mesh.normals;
                var world = new List<Vector3>(local.Length);
                foreach (Vector3 normal in local)
                {
                    world.Add(transform.TransformDirection(normal));
                }

                return world;
            }
        }
    }
}
