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
    /// </summary>
    public sealed class ConveyorBeltScroller : MonoBehaviour
    {
        // 벨트 텍스처가 걸린 URP 재질의 속성 이름. 타일링과 오프셋은 이 이름 뒤에 _ST를 붙인 속성에 담긴다.
        private const string TexturePropertyName = "_BaseMap";
        private const string TilingOffsetSuffix = "_ST";

        [Tooltip("벨트 윗면으로 칠 면의 법선과 위쪽 방향의 최소 내적. 1에 가까울수록 평평하게 위를 보는 면만 쓴다.")]
        [SerializeField, Range(0.5f, 1f)] private float _minTopDot = 0.9f;

        private readonly List<Belt> _belts = new List<Belt>();
        private MaterialPropertyBlock _block;
        private int _tilingOffsetId;

        private void Start()
        {
            _block = new MaterialPropertyBlock();
            _tilingOffsetId = Shader.PropertyToID(TexturePropertyName + TilingOffsetSuffix);

            foreach (ConveyorBase conveyor in FindObjectsByType<ConveyorBase>())
            {
                if (Belt.TryCreate(conveyor, TexturePropertyName, _minTopDot, out Belt belt))
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

            public static bool TryCreate(ConveyorBase conveyor, string textureProperty, float minTopDot, out Belt belt)
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
