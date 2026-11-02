using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 카메라가 달린 자리에 보이는 작은 카메라 본체(상자와 렌즈)를 만든다. 카메라 컴포넌트는 눈에 보이지 않아 어디에 달렸는지 알 수 없기 때문이다.
    /// 상자는 렌즈 뒤쪽(카메라 −Z)에 놓여 로봇 몸 바깥으로 튀어나온다. 충돌체는 만들지 않는다: 도구 링크의 물리와 충돌 검사에 끼면 안 된다.
    /// 카메라 시야와 위치 계산에는 관여하지 않는다(보이기만 한다).
    /// </summary>
    public static class ToolCameraHousing
    {
        private static readonly Vector3 BoxSizeM = new Vector3(0.035f, 0.03f, 0.03f);
        private static readonly Color BoxColor = new Color(0.1f, 0.1f, 0.12f);
        private static readonly Color LensColor = new Color(0.05f, 0.2f, 0.55f);
        private const float LensRadiusM = 0.009f;
        private const float LensThicknessM = 0.004f;

        /// <summary>카메라 오브젝트 아래에 본체를 만들고, 만든 렌더러들을 돌려준다(카메라 화면에서 빼는 데 쓴다).</summary>
        public static List<Renderer> Attach(Transform cameraTransform)
        {
            var renderers = new List<Renderer>();

            // 렌즈 면(카메라 자리)에서 뒤로 상자 깊이만큼. 렌즈 바로 뒤에서 시작해 시야를 가리지 않는다.
            GameObject box = Create(PrimitiveType.Cube, "ToolCameraBox", cameraTransform, new Vector3(0f, 0f, -BoxSizeM.z * 0.5f), Quaternion.identity, BoxSizeM, BoxColor);
            renderers.Add(box.GetComponent<Renderer>());

            // 원기둥은 Y축 방향이고 높이가 scale.y의 2배이다. X축으로 90도 눕혀 렌즈가 앞(+Z)을 보게 한다.
            GameObject lens = Create(
                PrimitiveType.Cylinder,
                "ToolCameraLens",
                cameraTransform,
                new Vector3(0f, 0f, -LensThicknessM * 0.5f),
                Quaternion.Euler(90f, 0f, 0f),
                new Vector3(LensRadiusM * 2f, LensThicknessM * 0.5f, LensRadiusM * 2f),
                LensColor);
            renderers.Add(lens.GetComponent<Renderer>());

            return renderers;
        }

        private static GameObject Create(PrimitiveType shape, string name, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;

            // 충돌체를 없앤다. 지우는 일은 프레임 끝에 일어나므로 그 전에 먼저 꺼서 물리에 끼지 않게 한다.
            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                if (Application.isPlaying)
                {
                    Object.Destroy(collider);
                }
                else
                {
                    Object.DestroyImmediate(collider);
                }
            }

            part.GetComponent<Renderer>().material.color = color;
            return part;
        }
    }
}
