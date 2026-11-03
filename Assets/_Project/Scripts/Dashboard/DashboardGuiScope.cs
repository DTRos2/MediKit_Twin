using System;
using UnityEngine;

namespace MediKitTwin.Dashboard
{
    /// <summary>
    /// 패널 하나를 그리는 동안만 대시보드 스킨과 화면 배율을 켜고, 끝나면 원래대로 돌려놓는다(using으로 쓴다).
    /// 다른 IMGUI 코드(인식 표시 등)가 우리 스킨과 배율에 영향받지 않게 하려는 것이다.
    /// 배율을 켜면 좌표가 기준 화면(1920×1080) 기준이 되므로, 패널은 virtualScreen 크기로 자리를 계산한다. OnGUI 안에서만 부른다.
    /// </summary>
    public readonly struct DashboardGuiScope : IDisposable
    {
        private readonly GUISkin _previousSkin;
        private readonly Matrix4x4 _previousMatrix;

        /// <param name="virtualScreen">배율을 적용한 화면 크기. 패널 자리 계산에 쓴다.</param>
        public DashboardGuiScope(out Vector2 virtualScreen)
        {
            float scale = DashboardScale.For(Screen.width, Screen.height);
            virtualScreen = new Vector2(Screen.width / scale, Screen.height / scale);

            _previousSkin = GUI.skin;
            _previousMatrix = GUI.matrix;
            GUI.skin = DashboardSkin.Get();
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
        }

        public void Dispose()
        {
            GUI.matrix = _previousMatrix;
            GUI.skin = _previousSkin;
        }
    }
}
