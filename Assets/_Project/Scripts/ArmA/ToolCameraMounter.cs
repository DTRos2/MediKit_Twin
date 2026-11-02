using System;
using System.Collections.Generic;
using UnityEngine;

namespace MediKitTwin.ArmA
{
    /// <summary>
    /// 도구 링크에 카메라를 단다. 도구 링크 몸통의 충돌체로 몸통 크기를 재고(ToolBodyBounds), 설정에 맞는 자리를 정해(ToolCameraPlacement),
    /// 카메라를 만든다(ToolCameraFeed). 어디에 달았는지 알린다. 장면을 읽는 일만 여기서 하고 계산은 각 클래스에 맡긴다.
    /// </summary>
    public static class ToolCameraMounter
    {
        /// <param name="robotRoot">로봇의 뿌리. 몸 숨기기를 켰으면 이 아래 렌더러를 카메라 화면에서 뺀다. 없으면 숨기지 않는다.</param>
        public static ToolCameraFeed Mount(ArticulationBody toolBody, Pose tcp, ToolCameraSettings settings, Transform robotRoot, Action<string> log)
        {
            bool hasBody = false;
            Bounds body = default;
            if (settings.Mount != ToolCameraMount.Manual)
            {
                hasBody = ToolBodyBounds.TryMeasure(BodyColliders(toolBody), tcp, out body);
                if (!hasBody)
                {
                    log("도구 링크에 충돌체가 없어 손 몸통 크기를 재지 못했다. 설정한 위치와 방향에 카메라를 단다.");
                }
            }

            Pose local = ToolCameraPlacement.Resolve(settings, hasBody, body);
            log(Describe(settings, hasBody, body, local));

            List<Renderer> hidden = settings.RenderImage && settings.HideOwnBody ? OwnBodyRenderers.Collect(robotRoot) : null;
            if (hidden != null)
            {
                log($"카메라 화면에서 로봇 몸 렌더러 {hidden.Count}개를 그리지 않는다(메인 화면과 충돌에는 영향 없음).");
            }

            ToolCameraFeed feed = ToolCameraFeed.Mount(toolBody.transform, tcp, local, settings, hidden);
            if (hidden != null)
            {
                log(DescribeGpuDrivenOptOut(feed));
            }

            return feed;
        }

        // URP의 GPU Resident Drawer가 켜져 있으면 숨기기가 먹으려면 숨길 렌더러를 Drawer에서 빼야 한다. 뺐는지, 뺄 방법이 없는지 알린다.
        private static string DescribeGpuDrivenOptOut(ToolCameraFeed feed)
        {
            return GpuDrivenRenderingOptOut.IsSupported
                ? $"숨길 렌더러 {feed.GpuDrivenOptOutCount}개를 GPU Resident Drawer에서 빼 일반 방식으로 그린다(카메라 화면에서 숨기려는 것)."
                : "GPU Resident Drawer에서 렌더러를 뺄 방법을 찾지 못했다. URP의 GPU Resident Drawer가 켜져 있으면 로봇 몸이 카메라 화면에 나올 수 있다.";
        }

        // 손가락처럼 자기 관절을 가진 하위 링크의 충돌체는 빼고, 도구 링크 자신에 붙은 충돌체(몸통)만 모은다.
        private static IEnumerable<Collider> BodyColliders(ArticulationBody toolBody)
        {
            foreach (Collider collider in toolBody.GetComponentsInChildren<Collider>())
            {
                if (collider.attachedArticulationBody == toolBody)
                {
                    yield return collider;
                }
            }
        }

        private static string Describe(ToolCameraSettings settings, bool hasBody, Bounds body, Pose local)
        {
            string where = !hasBody || settings.Mount == ToolCameraMount.Manual
                ? "설정한 위치"
                : settings.Mount == ToolCameraMount.HandCenter ? "그리퍼 가운데"
                : settings.Mount == ToolCameraMount.HandRightSide ? "손 몸통 오른쪽 면" : "손 몸통 왼쪽 면";
            Vector3 cm = local.position * 100f;
            string size = hasBody ? $" 몸통 크기(도구 끝점 기준) x {body.min.x * 100f:0.0}~{body.max.x * 100f:0.0}, y {body.min.y * 100f:0.0}~{body.max.y * 100f:0.0}, z {body.min.z * 100f:0.0}~{body.max.z * 100f:0.0}cm." : string.Empty;
            return $"카메라를 {where}에 달았다(도구 끝점 기준 x {cm.x:0.0}, y {cm.y:0.0}, z {cm.z:0.0}cm).{size}";
        }
    }
}
