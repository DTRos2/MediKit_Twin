using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 정해 둔 렌더러들을 URP의 GPU Resident Drawer에서 빼서 일반 방식으로 그리게 한다.
    /// 왜 필요한가: GPU Resident Drawer는 렌더러의 켜짐·꺼짐(forceRenderingOff 포함)을 프레임에 한 번, 그리기가 다 끝난 뒤(PostPostLateUpdate)에야 읽는다.
    /// 그래서 한 프레임 안에서 잠깐 껐다 켜고 그 사이에 카메라를 그리는 방식(RendererMuter)은 Drawer가 관리하는 렌더러에는 먹히지 않는다.
    /// 일반 방식으로 그리는 렌더러는 그릴 때마다 꺼짐 여부를 읽으므로 먹는다.
    /// 방법: Unity가 정한 공식 방법인 'Disallow GPU Driven Rendering' 컴포넌트를 렌더러 오브젝트에 붙인다. 이 컴포넌트는 패키지 안에서만 보이는(internal) 클래스라
    /// 타입 이름으로 찾아 붙인다. 이름의 패키지가 없으면(URP가 아니거나 구조가 바뀌면) 아무것도 하지 않는다(IsSupported가 false).
    /// 우리가 붙인 컴포넌트만 Restore에서 떼고, 이미 붙어 있던 것은 건드리지 않는다. MeshRenderer가 아닌 렌더러와 지워진 렌더러는 건너뛴다.
    /// 어떤 렌더러를 언제 끌지는 모른다(호출하는 쪽이 정한다).
    /// </summary>
    public sealed class GpuDrivenRenderingOptOut
    {
        private const string ComponentTypeName = "UnityEngine.Rendering.DisallowGPUDrivenRendering, Unity.RenderPipelines.GPUDriven.Runtime";

        private static readonly Type ComponentType = Type.GetType(ComponentTypeName);

        private readonly MeshRenderer[] _renderers;
        private readonly List<Component> _added = new List<Component>();
        private bool _isApplied;

        public GpuDrivenRenderingOptOut(IEnumerable<Renderer> renderers)
        {
            var meshes = new List<MeshRenderer>();
            if (renderers != null)
            {
                foreach (Renderer renderer in renderers)
                {
                    if (renderer is MeshRenderer mesh)
                    {
                        meshes.Add(mesh);
                    }
                }
            }

            _renderers = meshes.ToArray();
        }

        /// <summary>GPU Resident Drawer에서 뺄 방법(패키지의 컴포넌트)을 찾았으면 true.</summary>
        public static bool IsSupported => ComponentType != null;

        /// <summary>대상 MeshRenderer의 수.</summary>
        public int Count => _renderers.Length;

        /// <summary>Apply로 실제로 새로 뺀 렌더러의 수. 이미 빠져 있던 것은 세지 않는다.</summary>
        public int AppliedCount => _added.Count;

        public bool IsApplied => _isApplied;

        /// <summary>렌더러들을 GPU Resident Drawer에서 뺀다. 이미 뺐거나 뺄 방법이 없으면 아무 일도 하지 않는다.</summary>
        public void Apply()
        {
            if (_isApplied || !IsSupported)
            {
                return;
            }

            foreach (MeshRenderer renderer in _renderers)
            {
                if (renderer == null || renderer.GetComponent(ComponentType) != null)
                {
                    continue;
                }

                _added.Add(renderer.gameObject.AddComponent(ComponentType));
            }

            _isApplied = true;
        }

        /// <summary>Apply로 붙인 컴포넌트를 떼어 원래대로 돌린다. Apply하지 않았으면 아무 일도 하지 않는다.</summary>
        public void Restore()
        {
            if (!_isApplied)
            {
                return;
            }

            foreach (Component added in _added)
            {
                if (added == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(added);
                }
                else
                {
                    Object.DestroyImmediate(added);
                }
            }

            _added.Clear();
            _isApplied = false;
        }
    }
}
